using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Badges;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class BadgeProgressResetPatch
{
    [HarmonyPostfix]
    public static void CreateForNewRunPostfix(RunState __result)
    {
        foreach (var player in __result.Players)
        {
            BadgeProgressTracker.ResetProgress(player.NetId);
        }
    }
}
