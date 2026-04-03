using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Modifiers;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
[HarmonyPriority(Priority.Low)]
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

        if (ShouldSkipEndlessTransition(state))
        {
            return true;
        }

        MainFile.Logger.Info($"Endless mode: Intercepting final act transition. Current loop: {endlessModifier.YuWanCard_EndlessLoopCount}");

        __result = HandleEndlessTransition(__instance, state, endlessModifier);
        return false;
    }

    public static bool ShouldSkipEndlessTransition(RunState state)
    {
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

        MainFile.Logger.Info($"Endless mode: Transitioned to loop {endlessModifier.YuWanCard_EndlessLoopCount}");
    }

    private static int CalculateEliteBonus(int loopCount)
    {
        if (loopCount <= 0) return 0;
        
        int bonus = loopCount / 2;
        
        if (loopCount >= 5)
        {
            bonus += 1;
        }
        
        if (loopCount >= 10)
        {
            bonus += 2;
        }
        
        return Math.Max(1, bonus);
    }

    private static RunState? GetCurrentRunState()
    {
        var runManagerType = typeof(RunManager);
        var instanceProperty = runManagerType.GetProperty("Instance");
        if (instanceProperty == null)
        {
            return null;
        }

        var runManager = instanceProperty.GetValue(null) as RunManager;
        return runManager?.State;
    }

    private static void ProcessEliteBonus(MapPointTypeCounts __instance)
    {
        var runState = GetCurrentRunState();
        if (runState == null)
        {
            return;
        }

        var endlessModifier = EndlessModifier.GetEndlessModifier(runState);
        if (endlessModifier == null)
        {
            return;
        }

        int loopCount = endlessModifier.EffectiveLoopCount;
        if (loopCount <= 0)
        {
            return;
        }

        int eliteBonus = CalculateEliteBonus(loopCount);
        int newEliteCount = __instance.NumOfElites + eliteBonus;

        GameVersionCompat.TrySetNumOfElites(__instance, newEliteCount, eliteBonus, loopCount);
    }

    public static void ApplyMapPointTypeCountsPatches(Harmony harmony)
    {
        if (GameVersionCompat.MapPointTypeCountsNewConstructor != null)
        {
            var postfixMethod = AccessTools.Method(typeof(EndlessModePatch), nameof(NewConstructorPostfix));
            harmony.Patch(GameVersionCompat.MapPointTypeCountsNewConstructor, postfix: new HarmonyMethod(postfixMethod));
            MainFile.Logger.Info("Endless mode: Applied patch to MapPointTypeCounts(int, int) constructor");
        }

        if (GameVersionCompat.MapPointTypeCountsOldConstructor != null)
        {
            var postfixMethod = AccessTools.Method(typeof(EndlessModePatch), nameof(OldConstructorPostfix));
            harmony.Patch(GameVersionCompat.MapPointTypeCountsOldConstructor, postfix: new HarmonyMethod(postfixMethod));
            MainFile.Logger.Info("Endless mode: Applied patch to MapPointTypeCounts(Rng) constructor");
        }
    }

    private static void NewConstructorPostfix(MapPointTypeCounts __instance, int unknownCount, int restCount)
    {
        ProcessEliteBonus(__instance);
    }

    private static void OldConstructorPostfix(MapPointTypeCounts __instance, Rng rng)
    {
        ProcessEliteBonus(__instance);
    }
}
