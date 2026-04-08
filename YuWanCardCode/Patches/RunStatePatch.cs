using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunState))]
public static class RunStatePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(RunState.CreateForNewRun))]
    public static void CreateForNewRunPostfix(RunState __result)
    {
        if (__result.Players.Count <= 1) return;

        foreach (var player in __result.Players)
        {
            if (player.Character is Pig)
            {
                var roastPorkRelic = ModelDb.Relic<PigRoastPork>().ToMutable();
                roastPorkRelic.FloorAddedToDeck = 1;
                player.AddRelicInternal(roastPorkRelic);
            }
        }
    }
}
