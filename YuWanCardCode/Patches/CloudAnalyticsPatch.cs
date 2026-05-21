using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class CloudAnalyticsRunStartPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState __result)
    {
        CloudAnalyticsService.OnRunStarted(__result);
    }
}

[HarmonyPatch(typeof(RunManager), "UpdateRichPresence")]
public static class CloudAnalyticsRunObservedPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunManager __instance)
    {
        CloudAnalyticsService.OnRunObserved(__instance.State);
    }
}

[HarmonyPatch(typeof(RunManager), "OnEnded", [typeof(bool)])]
public static class CloudAnalyticsRunEndPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance, bool isVictory)
    {
        CloudAnalyticsService.OnRunEnded(__instance.State, isVictory);
    }
}
