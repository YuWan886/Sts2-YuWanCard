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

    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialOptions")]
    static void ModifyInitialOptions(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
            return;

        var options = __result.ToList();

        // Modifier screens have fewer than 3 options — skip
        if (options.Count < 3)
            return;

        _normalOptions[__instance] = options;

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
        if (YuWanCardConfig.EnableWhatIfRelics && _normalOptions.ContainsKey(neow))
        {
            SetEventState(neow, neow.InitialDescription, CreateWhatIfScreen(neow));
        }
        else if (_normalOptions.TryGetValue(neow, out var normalOpts))
        {
            SetEventState(neow, neow.InitialDescription, normalOpts);
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
                    if (_normalOptions.TryGetValue(neow, out var opts))
                        SetEventState(neow, neow.InitialDescription, opts);
                },
                mutable.Title,
                mutable.Description,
                mutable.Id.Entry + ".NEOW",
                mutable.HoverTipsExcludingRelic
            ).WithRelic(mutable));
        }

        options.Add(CreateSkipOption(neow, () =>
        {
            if (_normalOptions.TryGetValue(neow, out var opts))
                SetEventState(neow, neow.InitialDescription, opts);
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
}
