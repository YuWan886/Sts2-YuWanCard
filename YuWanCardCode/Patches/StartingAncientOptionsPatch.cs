using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using System.Reflection;
using YuWanCard.Config;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

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
        if (!ShouldInjectStartingAncientOptions(__instance))
        {
            return;
        }

        if (!YuWanCardConfig.EnableSevenCursesRing)
        {
            return;
        }

        if (HasStoredOriginalState(__instance))
        {
            return;
        }

        MainFile.Logger.Info($"[StartingAncientOptionsPatch] Injecting Seven Curses start options for {__instance.Id.Entry} via AncientEventModel.GenerateInitialOptionsWrapper originalCount={__result.Count}");
        var options = __result.ToList();

        _normalOptions[__instance] = options;
        _normalDescriptions[__instance] = __instance.InitialDescription;

        __result = CreateSevenCursesOptions(__instance);
        YuWanReflectionHelper.SetPrivateField(__instance, "_generatedOptions", __result.ToList());
    }

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
        RestoreNormalOptionsOrFinish(ancient);
    }

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
        var replacementOptions = StartingAncientOptionsPatch.CreateSevenCursesOptions(ancient);

        MainFile.Logger.Info($"[StartingAncientOptionsPatch] Injecting Seven Curses start options for {ancient.Id.Entry} via EventModel.SetEventState");
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

        if (!YuWanCardConfig.EnableSevenCursesRing)
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

        StartingAncientOptionsPatch.StoreOriginalModifiers(__instance, originalModifiers);
        YuWanReflectionHelper.SetPrivateField(runState, "<Modifiers>k__BackingField", optionModifiers);
    }

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
        return __exception;
    }
}
