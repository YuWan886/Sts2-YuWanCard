using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Core.Patches;

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
            if (player.Character is IYuWanCharacter yuWanChar)
            {
                var startingRelics = yuWanChar.MultiplayerStartingRelics;
                foreach (var relicModel in startingRelics)
                {
                    var relic = relicModel.ToMutable();
                    relic.FloorAddedToDeck = 1;
                    player.AddRelicInternal(relic);
                }
            }
        }
    }
}
