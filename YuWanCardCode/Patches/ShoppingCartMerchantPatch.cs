using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMerchantSlot))]
public static class NMerchantSlot_ShoppingCartPatch
{
    private static readonly Dictionary<NMerchantSlot, Button> _addToCartButtons = new();

    private static bool ClearAfterPurchase(MerchantEntry entry)
    {
        return YuWanReflectionHelper.CallPrivateMethod(entry, "ClearAfterPurchase");
    }

    private static bool UpdateVisual(NMerchantSlot slot)
    {
        return YuWanReflectionHelper.CallPrivateMethod(slot, "UpdateVisual");
    }

    [HarmonyPostfix]
    [HarmonyPatch("Initialize")]
    public static void AddShoppingCartButton(NMerchantSlot __instance)
    {
        if (!ShoppingCartManager.HasShoppingCart()) return;
        if (_addToCartButtons.ContainsKey(__instance)) return;
        if (__instance is NMerchantCardRemoval) return;

        var button = CreateAddToCartButton(__instance);
        if (button == null) return;

        _addToCartButtons[__instance] = button;
        __instance.AddChild(button);
    }

    [HarmonyPostfix]
    [HarmonyPatch("UpdateVisual")]
    public static void UpdateButtonVisibility(NMerchantSlot __instance)
    {
        if (!_addToCartButtons.TryGetValue(__instance, out var button)) return;

        var entry = __instance.Entry;
        button.Visible = entry != null && entry.IsStocked;
        button.Disabled = !ShoppingCartManager.HasShoppingCart() || 
                          ShoppingCartManager.GetCartData()?.IsFull == true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    public static void CleanupButton(NMerchantSlot __instance)
    {
        if (_addToCartButtons.TryGetValue(__instance, out var button))
        {
            _addToCartButtons.Remove(__instance);
            button.QueueFreeSafely();
        }
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
            ClearAfterPurchase(entry);
            UpdateVisual(slot);
            SfxCmd.Play("event:/sfx/ui/ui_card_reward_open");
        }
        else
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
        }
    }
}
