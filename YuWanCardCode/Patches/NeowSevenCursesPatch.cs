using System.Collections.Concurrent;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using YuWanCard.Relic;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Neow))]
class NeowSevenCursesPatch
{
    private static readonly ConcurrentDictionary<Neow, List<EventOption>> _originalOptions = new();
    private static readonly System.Reflection.MethodInfo? _setEventStateMethod = typeof(EventModel).GetMethod("SetEventState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, [typeof(LocString), typeof(IEnumerable<EventOption>)], null);

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

        var relic = ModelDb.Relic<RingOfSevenCurses>().ToMutable();
        relic.Owner = __instance.Owner;

        var sevenCursesOptions = new List<EventOption>
        {
            EventOption.FromRelic(
            relic,
            __instance,
            async () =>
            {
                await MegaCrit.Sts2.Core.Commands.RelicCmd.Obtain<RingOfSevenCurses>(__instance.Owner);
                if (_originalOptions.TryGetValue(__instance, out var options))
                {
                    _setEventStateMethod?.Invoke(__instance, [__instance.InitialDescription, options]);
                }
            },
            "YUWANCARD-SEVEN_CURSES_SELECT"
        )
        };

        LocString skipTitle = new("relics", "YUWANCARD-SEVEN_CURSES_SKIP.title");
        LocString skipDescription = new("relics", "YUWANCARD-SEVEN_CURSES_SKIP.description");
        sevenCursesOptions.Add(new EventOption(
            __instance,
            () =>
            {
                if (_originalOptions.TryGetValue(__instance, out var options))
                {
                    _setEventStateMethod?.Invoke(__instance, [__instance.InitialDescription, options]);
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
