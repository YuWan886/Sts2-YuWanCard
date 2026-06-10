using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
public static class WhatIfTreasureRelicPatch
{
    private sealed class TreasureRelicReplacementBox
    {
        public List<RelicModel> DisplayRelics { get; set; } = [];

        public List<RelicModel> CanonicalRelics { get; set; } = [];

        public List<RelicModel> OriginalRelics { get; set; } = [];

        public Dictionary<ModelId, RelicModel> ObtainMap { get; set; } = [];

        public int RemainingObtains { get; set; }
    }

    private static readonly ConditionalWeakTable<TreasureRoomRelicSynchronizer, TreasureRelicReplacementBox> ReplacementBoxes = new();
    private static readonly AccessTools.FieldRef<TreasureRoomRelicSynchronizer, List<RelicModel>?> CurrentRelicsField =
        AccessTools.FieldRefAccess<TreasureRoomRelicSynchronizer, List<RelicModel>?>("_currentRelics");
    private static readonly AccessTools.FieldRef<NTreasureRoomRelicCollection, List<NTreasureRoomRelicHolder>> HoldersInUseField =
        AccessTools.FieldRefAccess<NTreasureRoomRelicCollection, List<NTreasureRoomRelicHolder>>("_holdersInUse");
    private static readonly AccessTools.FieldRef<NTreasureRoomRelicCollection, IRunState> RunStateField =
        AccessTools.FieldRefAccess<NTreasureRoomRelicCollection, IRunState>("_runState");
    [HarmonyPostfix]
    public static void Postfix(TreasureRoomRelicSynchronizer __instance)
    {
        var runState = RunManager.Instance?.State;
        var currentRelics = CurrentRelicsField(__instance);
        if (runState == null || currentRelics == null || currentRelics.Count == 0)
        {
            ReplacementBoxes.Remove(__instance);
            return;
        }

        var source = FindUniformRelicSource(runState);
        if (source == null)
        {
            ReplacementBoxes.Remove(__instance);
            return;
        }

        var replacement = source.GetUniformRelic(runState);
        var displayRelics = new List<RelicModel>(currentRelics.Count);
        var canonicalRelics = new List<RelicModel>(currentRelics.Count);
        var obtainMap = new Dictionary<ModelId, RelicModel>(currentRelics.Count);
        for (int i = 0; i < currentRelics.Count; i++)
        {
            var canonicalRelic = replacement.ToMutable();
            var displayRelic = replacement.ToMutable();
            displayRelics.Add(displayRelic);
            canonicalRelics.Add(canonicalRelic);
            obtainMap[currentRelics[i].Id] = replacement;
            obtainMap[displayRelic.Id] = replacement;
            obtainMap[canonicalRelic.Id] = replacement;
        }

        ReplacementBoxes.Remove(__instance);
        ReplacementBoxes.Add(__instance, new TreasureRelicReplacementBox
        {
            DisplayRelics = displayRelics,
            CanonicalRelics = canonicalRelics,
            OriginalRelics = [.. currentRelics],
            ObtainMap = obtainMap
        });
        CurrentRelicsField(__instance) = canonicalRelics;
        MainFile.Logger.Info(
            $"[WhatIfTreasureRelicPatch] Remapped treasure relic display to {replacement.Id.Entry} x{displayRelics.Count} via {source.GetType().Name}");
    }

    private static IWhatIfUniformRelicSource? FindUniformRelicSource(IRunState runState)
    {
        foreach (var player in runState.Players)
        {
            foreach (var relic in player.Relics)
            {
                if (relic is IWhatIfUniformRelicSource source)
                {
                    return source;
                }
            }
        }

        return null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.InitializeRelics))]
    public static void InitializeRelicsPostfix(NTreasureRoomRelicCollection __instance)
    {
        var synchronizer = RunManager.Instance?.TreasureRoomRelicSynchronizer;
        if (synchronizer == null || !ReplacementBoxes.TryGetValue(synchronizer, out var box))
        {
            return;
        }

        var holders = HoldersInUseField(__instance);
        if (holders.Count == 0 || holders.Count != box.DisplayRelics.Count)
        {
            return;
        }

        var runState = RunStateField(__instance);
        for (int i = 0; i < holders.Count; i++)
        {
            holders[i].Initialize(box.DisplayRelics[i], runState);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), "AnimateRelicAwards")]
    public static void AnimateRelicAwardsPrefix(NTreasureRoomRelicCollection __instance, List<RelicPickingResult> results)
    {
        var synchronizer = RunManager.Instance?.TreasureRoomRelicSynchronizer;
        if (synchronizer == null || !ReplacementBoxes.TryGetValue(synchronizer, out var box))
        {
            return;
        }

        foreach (var result in results)
        {
            if (TryGetDisplayRelic(box, result.relic, out var displayRelic))
            {
                result.relic = displayRelic;
            }
        }

        box.RemainingObtains = results.Count(static result => result.type != RelicPickingResultType.Skipped);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(MegaCrit.Sts2.Core.Entities.Players.Player), typeof(int)])]
    public static void ObtainPrefix(ref RelicModel relic)
    {
        var synchronizer = RunManager.Instance?.TreasureRoomRelicSynchronizer;
        if (synchronizer == null || !ReplacementBoxes.TryGetValue(synchronizer, out var box))
        {
            return;
        }

        if (!box.ObtainMap.TryGetValue(relic.Id, out var canonicalReplacement))
        {
            return;
        }

        relic = canonicalReplacement.ToMutable();

        if (box.RemainingObtains > 0)
        {
            box.RemainingObtains--;
        }

        if (box.RemainingObtains == 0)
        {
            ReplacementBoxes.Remove(synchronizer);
        }
    }

    private static bool TryGetDisplayRelic(
        TreasureRelicReplacementBox box,
        RelicModel relic,
        out RelicModel displayRelic)
    {
        for (int i = 0; i < box.DisplayRelics.Count; i++)
        {
            if (ReferenceEquals(box.DisplayRelics[i], relic)
                || ReferenceEquals(box.CanonicalRelics[i], relic)
                || ReferenceEquals(box.OriginalRelics[i], relic))
            {
                displayRelic = box.DisplayRelics[i];
                return true;
            }
        }

        displayRelic = null!;
        return false;
    }
}
