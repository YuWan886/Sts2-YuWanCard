using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(RestSiteOption), "Description", MethodType.Getter)]
public static class SmithRestSiteDescriptionPatch
{
    private const string RestSiteUiTable = "rest_site_ui";
    private const string MultiDescriptionKey = "YUWANCARD-SMITH_MULTI.description";

    [HarmonyPostfix]
    public static void Postfix(RestSiteOption __instance, ref LocString __result)
    {
        if (__instance is not SmithRestSiteOption smith || !smith.IsEnabled)
        {
            return;
        }

        int count = SmithRestSiteOptionPatch.GetSmithCount(smith);
        if (count <= 1)
        {
            return;
        }

        EnsureEntry();
        var loc = new LocString(RestSiteUiTable, MultiDescriptionKey);
        loc.Add("Count", count);
        __result = loc;
    }

    // The mod's localization files live inside the packed .pck, and dotnet build does not
    // re-export it, so the multi-upgrade description is injected into the loc table at runtime.
    private static void EnsureEntry()
    {
        LocTable table = LocManager.Instance.GetTable(RestSiteUiTable);
        if (table.IsLocalKey(MultiDescriptionKey))
        {
            return;
        }

        string raw = LocManager.Instance.Language switch
        {
            "zhs" => "[gold]升级[/gold]你[gold]牌组[/gold]中的{Count}张牌。",
            "zht" => "[gold]升級[/gold]你[gold]牌組[/gold]中的{Count}張牌。",
            _ => "[gold]Upgrade[/gold] {Count} cards in your [gold]Deck[/gold]."
        };

        table.MergeWith(new Dictionary<string, string> { [MultiDescriptionKey] = raw });
    }
}

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
        int count = ExtraSmithCounts.TryGetValue(option, out var box) ? box.Value : 1;

        // Safety net: if the rest-site hook never touched this option instance, read the
        // extra smith count straight from the relic the owner actually holds.
        if (count <= 1 && option.Owner?.GetRelic<RingOfSevenCurses>() != null)
        {
            count = 2;
        }

        return count;
    }

    public static void AddSmithCount(SmithRestSiteOption option, int delta)
    {
        // Use the raw table value here: GetSmithCount() falls back to the held relic, and
        // feeding that back would count the ring bonus twice.
        var box = ExtraSmithCounts.GetOrCreateValue(option);
        int baseCount = Math.Max(1, box.Value);
        box.Value = baseCount + delta;
    }

    [HarmonyPatch("OnSelect")]
    [HarmonyPrefix]
    public static bool Prefix(SmithRestSiteOption __instance, ref Task<bool> __result)
    {
        var smithCount = GetSmithCount(__instance);
        MainFile.Logger.Info(
            $"[Smith] OnSelect tableCount={(ExtraSmithCounts.TryGetValue(__instance, out var box) ? box.Value : 1)} " +
            $"count={smithCount} ring={__instance.Owner?.GetRelic<RingOfSevenCurses>() != null} " +
            $"upgradable={__instance.Owner?.Deck.UpgradableCardCount.ToString() ?? "n/a"}");
        // 0.113 hardcodes a single upgrade inside the vanilla OnSelect, so every count above
        // one has to go through the mod's multi-card path, no matter how large the deck is.
        if (smithCount <= 1)
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
        var targetCount = Math.Min(smithCount, upgradableCount);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, minSelect, targetCount)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        MainFile.Logger.Info($"[Smith] FlexibleSelect min={minSelect} max={targetCount} upgradable={upgradableCount} smithCount={smithCount}");
        var selection = (await CardSelectCmd.FromDeckForUpgrade(owner, prefs)).ToList();
        MainFile.Logger.Info($"[Smith] FlexibleSelect picked={selection.Count}");

        if (selection.Count == 0)
        {
            return false;
        }

        var upgradedCards = new List<CardModel>(selection);
        foreach (var card in selection)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        // Safety net: if the selection screen ever hands back fewer cards than the smith count
        // allows (for example a single-select UI), keep offering one more upgrade until the
        // extra smith count from the relic is actually spent.
        while (upgradedCards.Count < targetCount && owner.Deck.UpgradableCardCount > 0)
        {
            var extraPrefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            };

            var extra = (await CardSelectCmd.FromDeckForUpgrade(owner, extraPrefs)).ToList();
            if (extra.Count == 0)
            {
                break;
            }

            foreach (var card in extra)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.None);
            }

            upgradedCards.AddRange(extra);
        }

        MainFile.Logger.Info($"[Smith] FlexibleSelect upgraded={upgradedCards.Count}");
        SetSelection(instance, upgradedCards);

        await Hook.AfterRestSiteSmith(owner.RunState, owner);
        return true;
    }
}
