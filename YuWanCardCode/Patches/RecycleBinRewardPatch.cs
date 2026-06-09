using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Rewards;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch]
public static class RecycleBinRewardPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardReward), nameof(CardReward.OnSkipped))]
    public static void QueueSkippedCardReward(CardReward __instance)
    {
        RecycleBin.QueueSkippedReward(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PotionReward), nameof(PotionReward.OnSkipped))]
    public static void QueueSkippedPotionReward(PotionReward __instance)
    {
        RecycleBin.QueueSkippedReward(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RelicReward), nameof(RelicReward.OnSkipped))]
    public static void QueueSkippedRelicReward(RelicReward __instance)
    {
        RecycleBin.QueueSkippedReward(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RewardSynchronizer), "HandleRewardObtainedMessage")]
    public static void MirrorSkippedReward(RewardObtainedMessage message, ulong senderId)
    {
        if (!message.wasSkipped)
        {
            return;
        }

        if (LocalContext.NetId == senderId)
        {
            return;
        }

        RecycleBin.QueueSyncedSkippedReward(message, senderId);
    }
}
