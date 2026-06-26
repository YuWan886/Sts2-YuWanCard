using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMerchantSlot))]
public static class NMerchantSlot_ShoppingCartPatch
{
    private static readonly Dictionary<NMerchantSlot, Button> _addToCartButtons = new();
    private static readonly HashSet<NMerchantInventory> _openInventories = [];

    private static bool UpdateVisual(NMerchantSlot slot)
    {
        return YuWanReflectionHelper.CallPrivateMethod(slot, "UpdateVisual");
    }

    public static void RegisterInventory(NMerchantInventory inventory)
    {
        _openInventories.Add(inventory);
    }

    public static void UnregisterInventory(NMerchantInventory inventory)
    {
        _openInventories.Remove(inventory);
    }

    [HarmonyPostfix]
    [HarmonyPatch("Initialize")]
    public static void AddShoppingCartButton(NMerchantSlot __instance)
    {
        var button = EnsureAddToCartButton(__instance);
        if (button != null)
        {
            ApplyButtonState(__instance, button);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnFocus")]
    public static void ClearStaleHoverTipBeforeFocus(NMerchantSlot __instance)
    {
        // Refreshing merchant navigation while the cursor is still over a slot can re-enter focus
        // without an intervening unfocus. Clear any stale tip first so CreateAndShow stays idempotent.
        NHoverTipSet.Remove(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("UpdateVisual")]
    public static void UpdateButtonVisibility(NMerchantSlot __instance)
    {
        var entry = __instance.Entry;
        bool isReserved = ShoppingCartManager.IsEntryReserved(entry);
        bool canAddToCart = ShoppingCartManager.CanAddToCart(entry);

        if (entry != null && entry.IsStocked)
        {
            __instance.Visible = !isReserved;
            __instance.MouseFilter = isReserved ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop;
        }

        var button = EnsureAddToCartButton(__instance);
        if (button == null)
        {
            return;
        }

        ApplyButtonState(__instance, button);
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    public static void CleanupButton(NMerchantSlot __instance)
    {
        if (TryGetLiveButton(__instance, out var button) && button != null)
        {
            _addToCartButtons.Remove(__instance);
            button.QueueFreeSafely();
        }
    }

    private static Button? EnsureAddToCartButton(NMerchantSlot slot)
    {
        if (slot is NMerchantCardRemoval)
        {
            return null;
        }

        if (TryGetLiveButton(slot, out var existingButton) && existingButton != null)
        {
            if (existingButton.GetParent() == null)
            {
                slot.AddChild(existingButton);
            }

            return existingButton;
        }

        if (!ShoppingCartManager.HasShoppingCart())
        {
            return null;
        }

        var button = CreateAddToCartButton(slot);
        if (button == null)
        {
            return null;
        }

        _addToCartButtons[slot] = button;
        slot.AddChild(button);
        return button;
    }

    private static bool TryGetLiveButton(NMerchantSlot slot, out Button? button)
    {
        if (_addToCartButtons.TryGetValue(slot, out button) && GodotObject.IsInstanceValid(button))
        {
            return true;
        }

        _addToCartButtons.Remove(slot);
        button = null;
        return false;
    }

    private static void ApplyButtonState(NMerchantSlot slot, Button button)
    {
        var entry = slot.Entry;
        bool isReserved = ShoppingCartManager.IsEntryReserved(entry);
        bool canAddToCart = ShoppingCartManager.CanAddToCart(entry);

        button.Visible = ShoppingCartManager.HasShoppingCart() &&
                         entry != null &&
                         entry.IsStocked &&
                         !isReserved &&
                         canAddToCart;
        button.Disabled = !ShoppingCartManager.HasShoppingCart() ||
                          ShoppingCartManager.GetCartData()?.IsFull == true ||
                          !canAddToCart ||
                          isReserved;
    }

    private static Button? CreateAddToCartButton(NMerchantSlot slot)
    {
        var button = new Button
        {
            Name = "ShoppingCartButton",
            Text = new LocString("settings_ui", "YUWANCARD-SHOPPING_CART.add_to_cart").GetRawText(),
            CustomMinimumSize = new Vector2(120, 30),
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0f,
            AnchorBottom = 0f,
            OffsetLeft = -80f,
            OffsetRight = 80f,
            OffsetTop = -10f,
            OffsetBottom = 30f
        };
        button.AddThemeFontSizeOverride("font_size", 24);
        button.Pressed += () => OnAddToCartPressed(slot);
        return button;
    }

    private static void OnAddToCartPressed(NMerchantSlot slot)
    {
        var entry = slot.Entry;
        if (entry == null || !entry.IsStocked) return;
        if (!ShoppingCartManager.CanAddToCart(entry))
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
            return;
        }

        var cartData = ShoppingCartManager.GetCartData();
        if (cartData == null || cartData.IsFull)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
            return;
        }

        bool added = entry switch
        {
            MerchantCardEntry cardEntry => ShoppingCartManager.AddToCart(cardEntry),
            MerchantRelicEntry relicEntry => ShoppingCartManager.AddToCart(relicEntry),
            MerchantPotionEntry potionEntry => ShoppingCartManager.AddToCart(potionEntry),
            _ => false
        };

        if (added)
        {
            RefreshOpenMerchantInventories();
            SfxCmd.Play("event:/sfx/ui/ui_card_reward_open");
        }
        else
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
        }
    }

    public static void RefreshOpenMerchantInventories()
    {
        foreach (var inventory in _openInventories.ToArray())
        {
            if (!GodotObject.IsInstanceValid(inventory))
            {
                _openInventories.Remove(inventory);
                continue;
            }

            foreach (var slot in inventory.GetAllSlots())
            {
                UpdateVisual(slot);
            }

            YuWanReflectionHelper.CallPrivateMethod(inventory, "UpdateNavigation");
        }
    }
}

[HarmonyPatch(typeof(NMerchantInventory))]
public static class NMerchantInventory_ShoppingCartPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory.Open))]
    public static void OnOpen(NMerchantInventory __instance)
    {
        NMerchantSlot_ShoppingCartPatch.RegisterInventory(__instance);
        NMerchantSlot_ShoppingCartPatch.RefreshOpenMerchantInventories();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Close")]
    public static void OnClose(NMerchantInventory __instance)
    {
        NMerchantSlot_ShoppingCartPatch.UnregisterInventory(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory._ExitTree))]
    public static void OnExitTree(NMerchantInventory __instance)
    {
        NMerchantSlot_ShoppingCartPatch.UnregisterInventory(__instance);
    }
}
