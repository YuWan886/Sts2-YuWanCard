using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using YuWanCard.Config;
using YuWanCard.RelicPools;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

/// <summary>
/// Unified Neow options patch. When enabled:
/// 1. Seven Curses option replaces the normal Neow screen
/// 2. After Seven Curses resolves, the What If screen appears (if enabled)
/// 3. After What If resolves (or if disabled), normal Neow options are restored
/// </summary>
[HarmonyPatch(typeof(AncientEventModel))]
class StartingAncientOptionsPatch
{
    private static readonly Dictionary<AncientEventModel, List<EventOption>> _normalOptions = [];
    private static readonly Dictionary<AncientEventModel, LocString> _normalDescriptions = [];
    private static readonly Dictionary<AncientEventModel, IReadOnlyList<ModifierModel>> _temporarilyFilteredModifiers = [];

    internal static void StoreOriginalModifiers(AncientEventModel eventModel, IReadOnlyList<ModifierModel> modifiers)
    {
        _temporarilyFilteredModifiers[eventModel] = modifiers;
    }

    internal static bool TryTakeOriginalModifiers(AncientEventModel eventModel, out IReadOnlyList<ModifierModel> modifiers)
    {
        if (!_temporarilyFilteredModifiers.TryGetValue(eventModel, out modifiers!))
        {
            modifiers = Array.Empty<ModifierModel>();
            return false;
        }

        _temporarilyFilteredModifiers.Remove(eventModel);
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialOptionsWrapper")]
    static void ModifyInitialOptions(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
    {
        // Polymorphic hook: fires for the starting ancient regardless of its concrete
        // type. Other mods (e.g. RandomizerMod) can replace the starting Neow with an
        // arbitrary AncientEventModel; gating on the floor boundary instead of the
        // Neow type keeps Seven Curses / What If working in that case.
        if (!ShouldInjectStartingAncientOptions(__instance))
        {
            return;
        }

        if (!YuWanCardConfig.EnableSevenCursesRing && !YuWanCardConfig.EnableWhatIfRelics)
        {
            return;
        }

        // The fallback SetEventState patch covers ancients that don't route option
        // generation through this wrapper. If we inject here, suppress that fallback.
        if (HasStoredOriginalState(__instance))
        {
            return;
        }

        MainFile.Logger.Info($"[StartingAncientOptionsPatch] Injecting start options for {__instance.Id.Entry} via AncientEventModel.GenerateInitialOptionsWrapper seven={YuWanCardConfig.EnableSevenCursesRing} whatIf={YuWanCardConfig.EnableWhatIfRelics} originalCount={__result.Count}");
        var options = __result.ToList();

        _normalOptions[__instance] = options;
        _normalDescriptions[__instance] = __instance.InitialDescription;

        if (YuWanCardConfig.EnableSevenCursesRing)
        {
            __result = CreateSevenCursesOptions(__instance);
        }
        else if (YuWanCardConfig.EnableWhatIfRelics)
        {
            __result = CreateWhatIfScreen(__instance);
        }

        // GenerateInitialOptionsWrapper has already cached the original options into
        // the private GeneratedOptions field (used by run-history tracking). Keep it
        // in sync with the options we actually display.
        YuWanReflectionHelper.SetPrivateField(__instance, "_generatedOptions", __result.ToList());
    }

    // ── Seven Curses ────────────────────────────────────────

    internal static IReadOnlyList<EventOption> CreateSevenCursesOptions(AncientEventModel ancient)
    {
        var selectTitle = new LocString("relics", "YUWANCARD-SEVEN_CURSES_SELECT.title");
        var selectDesc = new LocString("relics", "YUWANCARD-SEVEN_CURSES_SELECT.description");

        var options = new List<EventOption>
        {
            new EventOption(
                ancient,
                async () =>
                {
                    await RelicCmd.Obtain<RingOfSevenCurses>(ancient.Owner!);
                    ResolveSevenCurses(ancient);
                },
                selectTitle,
                selectDesc,
                "YUWANCARD-SEVEN_CURSES",
                Array.Empty<IHoverTip>()
            ).WithRelic<RingOfSevenCurses>(ancient.Owner!),

            CreateSkipOption(ancient, () => ResolveSevenCurses(ancient),
                "YUWANCARD-SEVEN_CURSES_SKIP", "relics")
        };

        return options;
    }

    private static void ResolveSevenCurses(AncientEventModel ancient)
    {
        if (YuWanCardConfig.EnableWhatIfRelics)
        {
            SetEventState(ancient, ancient.InitialDescription, CreateWhatIfScreen(ancient));
        }
        else
        {
            RestoreNormalOptionsOrFinish(ancient);
        }
    }

    // ── What If ─────────────────────────────────────────────

    internal static IReadOnlyList<EventOption> CreateWhatIfScreen(AncientEventModel ancient)
    {
        var selected = SelectDeterministicWhatIfRelics(ancient);
        return CreateWhatIfScreen(ancient, selected);
    }

    internal static bool IsShowingWhatIfScreen(AncientEventModel ancient)
    {
        return ancient.CurrentOptions.Any(static option =>
            string.Equals(option.TextKey, "YUWANCARD-WHAT_IF_SKIP", StringComparison.Ordinal));
    }

    internal static IReadOnlyList<EventOption> CreateRandomizedWhatIfScreen(AncientEventModel ancient)
    {
        var currentRelicIds = ancient.CurrentOptions
            .Select(static option => option.Relic?.Id.Entry)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var selected = SelectRandomWhatIfRelics(ancient, currentRelicIds);
        return CreateWhatIfScreen(ancient, selected);
    }

    private static IReadOnlyList<EventOption> CreateWhatIfScreen(AncientEventModel ancient, IReadOnlyList<RelicModel> selected)
    {
        var options = new List<EventOption>();
        foreach (var relic in selected)
        {
            var mutable = relic.ToMutable();
            var description = BuildRelicOptionDescription(mutable);
            options.Add(new EventOption(
                ancient,
                async () =>
                {
                    await RelicCmd.Obtain(mutable, ancient.Owner!);
                    RestoreNormalOptionsOrFinish(ancient);
                },
                mutable.Title,
                description,
                mutable.Id.Entry + ".NEOW",
                mutable.HoverTipsExcludingRelic
            ).WithRelic(mutable));
        }

        options.Add(CreateSkipOption(ancient, () =>
        {
            RestoreNormalOptionsOrFinish(ancient);
        }, "YUWANCARD-WHAT_IF_SKIP", "relics"));

        return options;
    }

    private static List<RelicModel> GetWhatIfCandidates(AncientEventModel ancient)
    {
        var pool = ModelDb.RelicPool<WhatIfRelicPool>();
        var runState = ancient.Owner?.RunState;
        bool isMultiplayer = runState?.Players.Count > 1;

        IEnumerable<RelicModel> candidates = pool.AllRelics
            .GroupBy(static relic => relic.Id.Entry, StringComparer.Ordinal)
            .Select(static group => group.First());

        if (isMultiplayer)
        {
            candidates = candidates.Where(static relic => relic is not WhatIfAllRelics);
        }

        return candidates.ToList();
    }

    private static IReadOnlyList<RelicModel> SelectDeterministicWhatIfRelics(AncientEventModel ancient)
    {
        var candidates = GetWhatIfCandidates(ancient);
        string seed = ancient.Owner?.RunState?.Rng.StringSeed ?? string.Empty;

        return candidates
            .OrderBy(relic => ComputeDeterministicSelectionKey(seed, relic.Id.Entry))
            .ThenBy(static relic => relic.Id.Entry, StringComparer.Ordinal)
            .Take(3)
            .ToList();
    }

    private static IReadOnlyList<RelicModel> SelectRandomWhatIfRelics(AncientEventModel ancient, ISet<string>? excludedRelicIds)
    {
        var candidates = GetWhatIfCandidates(ancient);
        if (excludedRelicIds is { Count: > 0 })
        {
            var filtered = candidates
                .Where(relic => !excludedRelicIds.Contains(relic.Id.Entry))
                .ToList();
            if (filtered.Count >= 3)
            {
                candidates = filtered;
            }
        }

        return candidates
            .UnstableShuffle(ancient.Rng)
            .Take(3)
            .ToList();
    }

    private static LocString BuildRelicOptionDescription(RelicModel relic)
    {
        var description = relic.Description;
        foreach (var dynamicVar in relic.DynamicVars.Values)
        {
            description.Add(dynamicVar);
        }

        return description;
    }

    private static ulong ComputeDeterministicSelectionKey(string seed, string relicId)
    {
        var bytes = Encoding.UTF8.GetBytes($"{seed}|YUWANCARD-NEOW-WHAT_IF|{relicId}");
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToUInt64(hash, 0);
    }

    // ── Helpers ─────────────────────────────────────────────

    private static EventOption CreateSkipOption(AncientEventModel ancient, Action onSkip, string key, string context)
    {
        var title = new LocString(context, $"{key}.title");
        var desc = new LocString(context, $"{key}.description");
        return new EventOption(
            ancient,
            () => { onSkip(); return Task.CompletedTask; },
            title,
            desc,
            key,
            Array.Empty<IHoverTip>()
        );
    }

    internal static bool SetEventState(EventModel eventModel, LocString description, IEnumerable<EventOption> options)
    {
        return YuWanReflectionHelper.CallPrivateMethod(eventModel, "SetEventState", description, options);
    }

    internal static bool ShouldInjectStartingAncientOptions(AncientEventModel ancient)
    {
        if (ancient.Owner?.RunState == null)
        {
            return false;
        }

        var runState = ancient.Owner.RunState;
        // The starting ancient (Neow, or whatever another mod swapped it for) is the
        // very first map point of act 0. TotalFloor counts visited map points, so it is
        // 1 once the starting ancient is recorded (observed at runtime). Use <= 1 to
        // also tolerate a floor-0 timing on the vanilla path.
        return runState.CurrentActIndex == 0 && runState.TotalFloor <= 1;
    }

    private static void RestoreNormalOptionsOrFinish(AncientEventModel ancient)
    {
        if (_normalOptions.TryGetValue(ancient, out var normalOpts) && normalOpts.Count > 0)
        {
            LocString restoreDescription = _normalDescriptions.TryGetValue(ancient, out var originalDescription)
                ? originalDescription
                : ancient.InitialDescription;
            SetEventState(ancient, restoreDescription, normalOpts);
            ClearStoredState(ancient);
            return;
        }

        // In custom mode, modifiers may exist but provide no Neow options.
        // In that case, conclude Neow cleanly after Seven Curses / What If resolves.
        LocString description = ancient is Neow
            ? new LocString("events", "NEOW.pages.DONE.description")
            : ancient.InitialDescription;
        YuWanReflectionHelper.CallPrivateMethod(ancient, "SetEventFinished", description);
        ClearStoredState(ancient);
    }

    internal static bool HasStoredOriginalState(AncientEventModel ancient)
    {
        return _normalOptions.ContainsKey(ancient);
    }

    internal static void StoreOriginalState(AncientEventModel ancient, LocString description, List<EventOption> options)
    {
        _normalOptions[ancient] = options;
        _normalDescriptions[ancient] = description;
    }

    private static void ClearStoredState(AncientEventModel ancient)
    {
        _normalOptions.Remove(ancient);
        _normalDescriptions.Remove(ancient);
    }
}

[HarmonyPatch]
static class StartingAncientSetEventStatePatch
{
    private static readonly HashSet<AncientEventModel> _suppressInjection = [];

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        return YuWanReflectionHelper.GetPrivateMethod(
            typeof(EventModel),
            "SetEventState",
            [typeof(LocString), typeof(IEnumerable<EventOption>)]);
    }

    [HarmonyPostfix]
    private static void Postfix(EventModel __instance, LocString description, IEnumerable<EventOption> eventOptions)
    {
        if (__instance is not AncientEventModel ancient || ancient is Neow)
        {
            return;
        }

        if (!ShouldInjectFallback(ancient))
        {
            return;
        }

        var originalOptions = eventOptions.ToList();
        if (originalOptions.Count == 0)
        {
            return;
        }

        StartingAncientOptionsPatch.StoreOriginalState(ancient, description, originalOptions);
        var replacementOptions = YuWanCardConfig.EnableSevenCursesRing
            ? StartingAncientOptionsPatch.CreateSevenCursesOptions(ancient)
            : StartingAncientOptionsPatch.CreateWhatIfScreen(ancient);

        MainFile.Logger.Info($"[StartingAncientOptionsPatch] Injecting start options for {ancient.Id.Entry} via EventModel.SetEventState");
        _suppressInjection.Add(ancient);
        try
        {
            StartingAncientOptionsPatch.SetEventState(ancient, description, replacementOptions);
        }
        finally
        {
            _suppressInjection.Remove(ancient);
        }
    }

    private static bool ShouldInjectFallback(AncientEventModel ancient)
    {
        if (_suppressInjection.Contains(ancient))
        {
            return false;
        }

        if (!StartingAncientOptionsPatch.ShouldInjectStartingAncientOptions(ancient))
        {
            return false;
        }

        if (!YuWanCardConfig.EnableSevenCursesRing && !YuWanCardConfig.EnableWhatIfRelics)
        {
            return false;
        }

        return !StartingAncientOptionsPatch.HasStoredOriginalState(ancient);
    }
}

[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
static class NeowRuntimeModifierFilterPatch
{
    [HarmonyPrefix]
    static void Prefix(Neow __instance)
    {
        if (__instance.Owner?.RunState == null)
        {
            return;
        }

        var runState = __instance.Owner.RunState;
        var originalModifiers = runState.Modifiers;
        var optionModifiers = originalModifiers
            .Where(modifier => modifier.GenerateNeowOption(__instance) != null)
            .ToList();

        // Preserve cross-mod modifiers that must not be temporarily removed
        // from RunState.Modifiers — doing so can corrupt their initialization
        // state (e.g. Hextech Mayhem modifier relies on being present during
        // Neow to set up its act selection flow).
        foreach (var modifier in originalModifiers)
        {
            if (IsCrossModCriticalModifier(modifier) && !optionModifiers.Contains(modifier))
            {
                optionModifiers.Add(modifier);
            }
        }

        if (optionModifiers.Count == originalModifiers.Count)
        {
            return;
        }

        MainFile.Logger.Info(
            $"[BalatroDebug] NeowRuntimeModifierFilter prefix original=[{string.Join(", ", originalModifiers.Select(static m => m.Id.Entry))}] filtered=[{string.Join(", ", optionModifiers.Select(static m => m.Id.Entry))}]");
        StartingAncientOptionsPatch.StoreOriginalModifiers(__instance, originalModifiers);
        YuWanReflectionHelper.SetPrivateField(runState, "<Modifiers>k__BackingField", optionModifiers);
    }

    /// <summary>
    /// Returns true for modifiers owned by other mods that are known to rely
    /// on being present in RunState.Modifiers during Neow initialization.
    /// Temporarily removing them corrupts their internal state.
    /// </summary>
    private static bool IsCrossModCriticalModifier(ModifierModel modifier)
    {
        string entry = modifier.Id.Entry;
        return entry.Contains("HEXTECH", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("MAYHEM", StringComparison.OrdinalIgnoreCase);
    }

    [HarmonyFinalizer]
    static Exception? Finalizer(Neow __instance, Exception? __exception)
    {
        if (__instance.Owner?.RunState == null)
        {
            return __exception;
        }

        if (!StartingAncientOptionsPatch.TryTakeOriginalModifiers(__instance, out var originalModifiers))
        {
            return __exception;
        }

        YuWanReflectionHelper.SetPrivateField(__instance.Owner.RunState, "<Modifiers>k__BackingField", originalModifiers);
        MainFile.Logger.Info(
            $"[BalatroDebug] NeowRuntimeModifierFilter restore modifiers=[{string.Join(", ", originalModifiers.Select(static m => m.Id.Entry))}] exception={__exception?.GetType().Name ?? "null"}");
        return __exception;
    }
}
