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

        public List<RelicModel> PickingRelics { get; set; } = [];

        public List<RelicModel> ReplacementRelics { get; set; } = [];

        public List<RelicModel> OriginalRelics { get; set; } = [];

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
        var pickingRelics = new List<RelicModel>(currentRelics.Count);
        var replacementRelics = new List<RelicModel>(currentRelics.Count);
        for (int i = 0; i < currentRelics.Count; i++)
        {
            var pickingRelic = replacement.ToMutable();
            var displayRelic = replacement.ToMutable();
            pickingRelics.Add(pickingRelic);
            displayRelics.Add(displayRelic);
            replacementRelics.Add(replacement);
        }

        ReplacementBoxes.Remove(__instance);
        ReplacementBoxes.Add(__instance, new TreasureRelicReplacementBox
        {
            DisplayRelics = displayRelics,
            PickingRelics = pickingRelics,
            ReplacementRelics = replacementRelics,
            OriginalRelics = [.. currentRelics]
        });
        CurrentRelicsField(__instance) = pickingRelics;
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
        if (holders.Count == 0)
        {
            return;
        }

        var activeHolders = new List<NTreasureRoomRelicHolder>(box.DisplayRelics.Count);
        foreach (var holder in holders)
        {
            if (holder.Visible)
            {
                activeHolders.Add(holder);
            }
        }

        if (activeHolders.Count != box.DisplayRelics.Count)
        {
            activeHolders.Clear();
            for (int i = 0; i < holders.Count && i < box.DisplayRelics.Count; i++)
            {
                activeHolders.Add(holders[i]);
            }
        }

        if (activeHolders.Count != box.DisplayRelics.Count)
        {
            MainFile.Logger.Warn(
                $"[WhatIfTreasureRelicPatch] Failed to remap treasure holders: active={activeHolders.Count}, display={box.DisplayRelics.Count}, total={holders.Count}");
            return;
        }

        HoldersInUseField(__instance) = activeHolders;
        var runState = RunStateField(__instance);
        for (int i = 0; i < activeHolders.Count; i++)
        {
            activeHolders[i].Relic.Model = box.DisplayRelics[i];
            activeHolders[i].Initialize(box.DisplayRelics[i], runState);
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
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.ToMutable))]
    public static bool RelicToMutablePrefix(RelicModel __instance, ref RelicModel __result)
    {
        var synchronizer = RunManager.Instance?.TreasureRoomRelicSynchronizer;
        if (synchronizer == null || !ReplacementBoxes.TryGetValue(synchronizer, out var box))
        {
            return true;
        }

        if (!TryGetCanonicalReplacement(box, __instance, out var canonicalReplacement))
        {
            return true;
        }

        __result = canonicalReplacement.ToMutable();
        return false;
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

        if (!TryGetCanonicalReplacement(box, relic, out var canonicalReplacement))
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
                || ReferenceEquals(box.PickingRelics[i], relic)
                || ReferenceEquals(box.OriginalRelics[i], relic))
            {
                displayRelic = box.DisplayRelics[i];
                return true;
            }
        }

        displayRelic = null!;
        return false;
    }

    private static bool TryGetCanonicalReplacement(
        TreasureRelicReplacementBox box,
        RelicModel relic,
        out RelicModel canonicalReplacement)
    {
        for (int i = 0; i < box.ReplacementRelics.Count; i++)
        {
            if (ReferenceEquals(box.DisplayRelics[i], relic)
                || ReferenceEquals(box.PickingRelics[i], relic)
                || ReferenceEquals(box.OriginalRelics[i], relic))
            {
                canonicalReplacement = box.ReplacementRelics[i];
                return true;
            }
        }

        canonicalReplacement = null!;
        return false;
    }

}
