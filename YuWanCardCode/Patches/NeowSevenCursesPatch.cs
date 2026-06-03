using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
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
[HarmonyPatch(typeof(Neow))]
class NeowSevenCursesPatch
{
    private static readonly Dictionary<Neow, List<EventOption>> _normalOptions = [];
    private static readonly Dictionary<Neow, LocString> _normalDescriptions = [];
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
    [HarmonyPatch("GenerateInitialOptions")]
    static void ModifyInitialOptions(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
            return;

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
    }

    // ── Seven Curses ────────────────────────────────────────

    private static IReadOnlyList<EventOption> CreateSevenCursesOptions(Neow neow)
    {
        var selectTitle = new LocString("relics", "YUWANCARD-SEVEN_CURSES_SELECT.title");
        var selectDesc = new LocString("relics", "YUWANCARD-SEVEN_CURSES_SELECT.description");

        var options = new List<EventOption>
        {
            new EventOption(
                neow,
                async () =>
                {
                    await RelicCmd.Obtain<RingOfSevenCurses>(neow.Owner!);
                    ResolveSevenCurses(neow);
                },
                selectTitle,
                selectDesc,
                "YUWANCARD-SEVEN_CURSES",
                Array.Empty<IHoverTip>()
            ).WithRelic<RingOfSevenCurses>(neow.Owner!),

            CreateSkipOption(neow, () => ResolveSevenCurses(neow),
                "YUWANCARD-SEVEN_CURSES_SKIP", "relics")
        };

        return options;
    }

    private static void ResolveSevenCurses(Neow neow)
    {
        if (YuWanCardConfig.EnableWhatIfRelics)
        {
            SetEventState(neow, neow.InitialDescription, CreateWhatIfScreen(neow));
        }
        else
        {
            RestoreNormalOptionsOrFinish(neow);
        }
    }

    // ── What If ─────────────────────────────────────────────

    private static IReadOnlyList<EventOption> CreateWhatIfScreen(Neow neow)
    {
        var pool = ModelDb.RelicPool<WhatIfRelicPool>();
        var selected = pool.AllRelics.Distinct().ToList().UnstableShuffle(neow.Rng).Take(3);

        var options = new List<EventOption>();
        foreach (var relic in selected)
        {
            var mutable = relic.ToMutable();
            options.Add(new EventOption(
                neow,
                async () =>
                {
                    await RelicCmd.Obtain(mutable, neow.Owner!);
                    RestoreNormalOptionsOrFinish(neow);
                },
                mutable.Title,
                mutable.Description,
                mutable.Id.Entry + ".NEOW",
                mutable.HoverTipsExcludingRelic
            ).WithRelic(mutable));
        }

        options.Add(CreateSkipOption(neow, () =>
        {
            RestoreNormalOptionsOrFinish(neow);
        }, "YUWANCARD-WHAT_IF_SKIP", "relics"));

        return options;
    }

    // ── Helpers ─────────────────────────────────────────────

    private static EventOption CreateSkipOption(Neow neow, Action onSkip, string key, string context)
    {
        var title = new LocString(context, $"{key}.title");
        var desc = new LocString(context, $"{key}.description");
        return new EventOption(
            neow,
            () => { onSkip(); return Task.CompletedTask; },
            title,
            desc,
            key,
            Array.Empty<IHoverTip>()
        );
    }

    private static bool SetEventState(EventModel eventModel, LocString description, IEnumerable<EventOption> options)
    {
        return YuWanReflectionHelper.CallPrivateMethod(eventModel, "SetEventState", description, options);
    }

    private static void RestoreNormalOptionsOrFinish(Neow neow)
    {
        if (_normalOptions.TryGetValue(neow, out var normalOpts) && normalOpts.Count > 0)
        {
            LocString description = _normalDescriptions.TryGetValue(neow, out var originalDescription)
                ? originalDescription
                : neow.InitialDescription;
            SetEventState(neow, description, normalOpts);
            return;
        }

        // In custom mode, modifiers may exist but provide no Neow options.
        // In that case, conclude Neow cleanly after Seven Curses / What If resolves.
        YuWanReflectionHelper.CallPrivateMethod(neow, "SetEventFinished", new LocString("events", "NEOW.pages.DONE.description"));
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

        if (optionModifiers.Count == originalModifiers.Count)
        {
            return;
        }

        MainFile.Logger.Info(
            $"[BalatroDebug] NeowRuntimeModifierFilter prefix original=[{string.Join(", ", originalModifiers.Select(static m => m.Id.Entry))}] filtered=[{string.Join(", ", optionModifiers.Select(static m => m.Id.Entry))}]");
        NeowSevenCursesPatch.StoreOriginalModifiers(__instance, originalModifiers);
        YuWanReflectionHelper.SetPrivateField(runState, "<Modifiers>k__BackingField", optionModifiers);
    }

    [HarmonyFinalizer]
    static Exception? Finalizer(Neow __instance, Exception? __exception)
    {
        if (__instance.Owner?.RunState == null)
        {
            return __exception;
        }

        if (!NeowSevenCursesPatch.TryTakeOriginalModifiers(__instance, out var originalModifiers))
        {
            return __exception;
        }

        YuWanReflectionHelper.SetPrivateField(__instance.Owner.RunState, "<Modifiers>k__BackingField", originalModifiers);
        MainFile.Logger.Info(
            $"[BalatroDebug] NeowRuntimeModifierFilter restore modifiers=[{string.Join(", ", originalModifiers.Select(static m => m.Id.Entry))}] exception={__exception?.GetType().Name ?? "null"}");
        return __exception;
    }
}
