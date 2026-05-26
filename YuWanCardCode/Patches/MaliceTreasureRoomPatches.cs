using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using YuWanCard.Malice;
using YuWanCard.Relics.Malice;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
public static class PrideMaliceBossRewardPatch
{
    [HarmonyPostfix]
    public static void Postfix(TreasureRoomRelicSynchronizer __instance)
    {
        if (!MaliceHelper.HasMalice(1))
            return;

        var relics = AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_currentRelics")
            .GetValue(__instance) as List<RelicModel>;
        if (relics == null)
            return;

        relics.Add(ModelDb.Relic<PrideMalice>());
    }
}
