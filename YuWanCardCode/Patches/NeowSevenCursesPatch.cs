using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using YuWanCard.Relic;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
class NeowSevenCursesPatch
{
    private static IReadOnlyList<EventOption>? _originalOptions;

    [HarmonyPostfix]
    static void AddSevenCursesOption(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        _originalOptions = __result;

        var relic = ModelDb.Relic<RingOfSevenCurses>().ToMutable();
        relic.Owner = __instance.Owner;
        
        EventOption sevenCursesOption = EventOption.FromRelic(
            relic,
            __instance,
            () => SelectSevenCurses(__instance),
            "YUWANCARD-SEVEN_CURSES_SELECT"
        );
        
        LocString skipTitle = new("relics", "YUWANCARD-SEVEN_CURSES_SKIP.title");
        LocString skipDescription = new("relics", "YUWANCARD-SEVEN_CURSES_SKIP.description");
        EventOption skipOption = new(
            __instance,
            () => SkipSevenCurses(__instance),
            skipTitle,
            skipDescription,
            "YUWANCARD-SEVEN_CURSES_SKIP",
            Array.Empty<IHoverTip>()
        );
        
        __result = [sevenCursesOption, skipOption];
    }

    private static async Task SelectSevenCurses(Neow neow)
    {
        if (neow.Owner != null)
        {
            await RelicCmd.Obtain<RingOfSevenCurses>(neow.Owner);
        }
        
        if (_originalOptions != null && _originalOptions.Count > 0)
        {
            MethodInfo? setEventStateMethod = typeof(EventModel).GetMethod("SetEventState", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(LocString), typeof(IEnumerable<EventOption>)], null);
            setEventStateMethod?.Invoke(neow, [neow.InitialDescription, _originalOptions]);
        }
    }

    private static Task SkipSevenCurses(Neow neow)
    {
        if (_originalOptions != null && _originalOptions.Count > 0)
        {
            MethodInfo? setEventStateMethod = typeof(EventModel).GetMethod("SetEventState", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(LocString), typeof(IEnumerable<EventOption>)], null);
            setEventStateMethod?.Invoke(neow, [neow.InitialDescription, _originalOptions]);
        }
        return Task.CompletedTask;
    }
}
