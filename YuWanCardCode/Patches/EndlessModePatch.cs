using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
public class EndlessModePatch
{
    private static readonly MethodInfo StateGetter = AccessTools.PropertyGetter(typeof(RunManager), "State");
    private static readonly MethodInfo ClearScreensMethod = AccessTools.Method(typeof(RunManager), "ClearScreens");
    private static readonly MethodInfo ExitCurrentRoomsMethod = AccessTools.Method(typeof(RunManager), "ExitCurrentRooms");
    private static readonly MethodInfo FadeInMethod = AccessTools.Method(typeof(RunManager), "FadeIn", [typeof(bool)]);

    [HarmonyPrefix]
    public static bool Prefix(RunManager __instance, ref Task __result)
    {
        var state = (RunState?)StateGetter.Invoke(__instance, null);
        if (state == null)
        {
            return true;
        }

        var endlessModifier = EndlessModifier.GetEndlessModifier(state);
        if (endlessModifier == null)
        {
            return true;
        }

        if (state.CurrentActIndex < state.Acts.Count - 1)
        {
            endlessModifier.IncrementActCount();
            return true;
        }

        MainFile.Logger.Info($"Endless mode: Intercepting final act transition. Current loop: {endlessModifier.EndlessLoopCount}");

        __result = HandleEndlessTransition(__instance, state, endlessModifier);
        return false;
    }

    private static async Task HandleEndlessTransition(RunManager runManager, RunState state, EndlessModifier endlessModifier)
    {
        endlessModifier.IncrementLoopCount();

        if (TestMode.IsOff && NGame.Instance?.Transition != null)
        {
            await NGame.Instance.Transition.RoomFadeOut();
        }

        ClearScreensMethod.Invoke(runManager, null);
        await (Task)ExitCurrentRoomsMethod.Invoke(runManager, null)!;

        runManager.GenerateRooms();
        
        foreach (var act in state.Acts)
        {
            MainFile.Logger.Debug($"Endless mode: Act {act.Id.Entry} - Ancient: {act.Ancient?.Id.Entry}, Boss: {act.BossEncounter?.Id.Entry}");
        }

        await runManager.SetActInternal(0);

        NMapScreen.Instance?.InitMarker(state.Map.StartingMapPoint.coord);
        await runManager.EnterMapCoord(state.Map.StartingMapPoint.coord);
        NMapScreen.Instance?.RefreshAllMapPointVotes();

        await (Task)FadeInMethod.Invoke(runManager, [true])!;

        MainFile.Logger.Info($"Endless mode: Transitioned to loop {endlessModifier.EndlessLoopCount}");
    }
}
