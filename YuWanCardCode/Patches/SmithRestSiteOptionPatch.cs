using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(SmithRestSiteOption))]
public static class SmithRestSiteOptionPatch
{
    [HarmonyPatch("OnSelect")]
    [HarmonyPrefix]
    public static bool Prefix(SmithRestSiteOption __instance, ref Task<bool> __result)
    {
        __result = OnSelectWithFlexibleCount(__instance);
        return false;
    }

    [HarmonyPatch("DoLocalPostSelectVfx")]
    [HarmonyPrefix]
    public static bool DoLocalPostSelectVfxPrefix(SmithRestSiteOption __instance, CancellationToken ct)
    {
        return __instance.Owner != null;
    }

    private static async Task<bool> OnSelectWithFlexibleCount(SmithRestSiteOption instance)
    {
        var owner = (Player)typeof(RestSiteOption)
            .GetProperty("Owner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(instance)!;

        var smithCount = instance.SmithCount;

        var upgradableCount = owner.Deck.UpgradableCardCount;
        if (upgradableCount == 0)
        {
            return false;
        }

        var minSelect = Math.Min(1, upgradableCount);
        var maxSelect = Math.Min(smithCount, upgradableCount);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, minSelect, maxSelect)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var selection = await CardSelectCmd.FromDeckForUpgrade(owner, prefs);
        if (!selection.Any())
        {
            return false;
        }

        foreach (var card in selection)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        await Hook.AfterRestSiteSmith(owner.RunState, owner);
        return true;
    }
}
