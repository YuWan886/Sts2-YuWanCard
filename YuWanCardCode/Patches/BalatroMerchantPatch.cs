using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.UI;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class BalatroMerchantPatch
{
    private const string ExtensionName = "YuWanBalatroMerchantExtension";

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory._Ready))]
    public static void OnReady(NMerchantInventory __instance)
    {
        EnsureExtension(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory.Open))]
    public static void OnOpen(NMerchantInventory __instance)
    {
        EnsureExtension(__instance)?.RefreshForOpen();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Close")]
    public static void OnClose(NMerchantInventory __instance)
    {
        EnsureExtension(__instance)?.OnInventoryClosed();
    }

    private static NBalatroMerchantExtension? EnsureExtension(NMerchantInventory inventory)
    {
        if (inventory.FindChild(ExtensionName, recursive: true, owned: false) is NBalatroMerchantExtension existing)
        {
            return existing;
        }

        Control? slotsContainer = inventory.GetNodeOrNull<Control>("%SlotsContainer");
        if (slotsContainer == null)
        {
            return null;
        }

        NBalatroMerchantExtension extension = new();
        extension.Initialize(inventory);
        slotsContainer.AddChild(extension);
        slotsContainer.MoveChild(extension, slotsContainer.GetChildCount() - 1);
        return extension;
    }
}
