using System.Collections.Concurrent;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Neow))]
class NeowSevenCursesPatch
{
    private static readonly ConcurrentDictionary<Neow, List<EventOption>> _originalOptions = new();

    private static bool SetEventState(EventModel eventModel, LocString description, IEnumerable<EventOption> options)
    {
        return YuWanReflectionHelper.CallPrivateMethod(eventModel, "SetEventState", description, options);
    }

    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialOptions")]
    static void AddSevenCursesOption(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        var originalOptions = __result.ToList();
        _originalOptions[__instance] = originalOptions;

        LocString selectTitle = new("relics", "YUWANCARD-SEVEN_CURSES_SELECT.title");
        LocString selectDescription = new("relics", "YUWANCARD-SEVEN_CURSES_SELECT.description");

        var sevenCursesOptions = new List<EventOption>
        {
            new EventOption(
                __instance,
                async () =>
                {
                    await MegaCrit.Sts2.Core.Commands.RelicCmd.Obtain<RingOfSevenCurses>(__instance.Owner);
                    if (_originalOptions.TryGetValue(__instance, out var options))
                    {
                        SetEventState(__instance, __instance.InitialDescription, options);
                    }
                },
                selectTitle,
                selectDescription,
                "YUWANCARD-SEVEN_CURSES",
                Array.Empty<MegaCrit.Sts2.Core.HoverTips.IHoverTip>()
            ).WithRelic<RingOfSevenCurses>(__instance.Owner)
        };

        LocString skipTitle = new("relics", "YUWANCARD-SEVEN_CURSES_SKIP.title");
        LocString skipDescription = new("relics", "YUWANCARD-SEVEN_CURSES_SKIP.description");
        sevenCursesOptions.Add(new EventOption(
            __instance,
            () =>
            {
                if (_originalOptions.TryGetValue(__instance, out var options))
                {
                    SetEventState(__instance, __instance.InitialDescription, options);
                }
                return Task.CompletedTask;
            },
            skipTitle,
            skipDescription,
            "YUWANCARD-SEVEN_CURSES_SKIP",
            Array.Empty<MegaCrit.Sts2.Core.HoverTips.IHoverTip>()
        ));

        __result = sevenCursesOptions;
    }
}
