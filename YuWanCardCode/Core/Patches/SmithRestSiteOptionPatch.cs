using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YuWanCard.Utils;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(SmithRestSiteOption))]
public static class SmithRestSiteOptionPatch
{
    private static bool SetSelection(SmithRestSiteOption instance, IEnumerable<CardModel> selection)
    {
        return YuWanReflectionHelper.SetPrivateField(instance, "_selection", selection);
    }

    // 0.113 removed SmithRestSiteOption.SmithCount and hardcodes a single upgrade, so the mod
    // keeps its own per-option count here.
    private static readonly ConditionalWeakTable<SmithRestSiteOption, StrongBox<int>> ExtraSmithCounts = new();

    public static int GetSmithCount(SmithRestSiteOption option)
    {
        return ExtraSmithCounts.TryGetValue(option, out var box) ? box.Value : 1;
    }

    public static void AddSmithCount(SmithRestSiteOption option, int delta)
    {
        var box = ExtraSmithCounts.GetOrCreateValue(option);
        box.Value = Math.Max(1, GetSmithCount(option) + delta);
    }

    [HarmonyPatch("OnSelect")]
    [HarmonyPrefix]
    public static bool Prefix(SmithRestSiteOption __instance, ref Task<bool> __result)
    {
        var smithCount = GetSmithCount(__instance);
        if (smithCount <= 1
            || (__instance.Owner is { } owner && owner.Deck.UpgradableCardCount >= smithCount))
        {
            return true;
        }

        __result = OnSelectWithFlexibleCount(__instance, smithCount);
        return false;
    }

    [HarmonyPatch("DoLocalPostSelectVfx")]
    [HarmonyPrefix]
    public static bool DoLocalPostSelectVfxPrefix(SmithRestSiteOption __instance, CancellationToken ct)
    {
        return __instance.Owner != null;
    }

    private static async Task<bool> OnSelectWithFlexibleCount(SmithRestSiteOption instance, int smithCount)
    {
        var owner = instance.Owner;
        if (owner == null)
        {
            return false;
        }

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
        
        SetSelection(instance, selection);
        
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
