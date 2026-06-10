using System.Linq;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch]
public static class RecycleBinRewardPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardReward), nameof(CardReward.OnSkipped))]
    public static void QueueSkippedCardReward(CardReward __instance)
    {
        RecycleBin.QueueSkippedReward(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardReward), "OnSelect")]
    public static void CaptureSkippedCardsBeforeSelection(CardReward __instance, out List<CardModel> __state)
    {
        __state = [.. __instance.Cards];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardReward), "OnSelect")]
    public static void QueueSkippedCardsAfterSelection(CardReward __instance, List<CardModel> __state, ref Task<bool> __result)
    {
        __result = QueueSkippedCardsAfterSelectionAsync(__instance, __state, __result);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardReward), nameof(CardReward.Reroll))]
    public static void QueueSkippedCardRewardBeforeReroll(CardReward __instance)
    {
        RecycleBin.QueueSkippedCards(__instance.Player, __instance.Cards, nameof(CardReward.Reroll));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PotionReward), nameof(PotionReward.OnSkipped))]
    public static void QueueSkippedPotionReward(PotionReward __instance)
    {
        RecycleBin.QueueSkippedReward(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicReward), nameof(RelicReward.OnSkipped))]
    public static void QueueSkippedRelicReward(RelicReward __instance)
    {
        RecycleBin.QueueSkippedReward(__instance);
    }

    private static async Task<bool> QueueSkippedCardsAfterSelectionAsync(
        CardReward reward,
        List<CardModel> originalCards,
        Task<bool> originalTask)
    {
        bool removeReward = await originalTask;
        if (!removeReward)
        {
            return false;
        }

        HashSet<ModelId> remainingCardIds = reward.Cards
            .Select(card => card.Id)
            .ToHashSet();
        List<CardModel> skippedCards = originalCards
            .Where(card => remainingCardIds.Contains(card.Id))
            .ToList();

        RecycleBin.QueueSkippedCards(reward.Player, skippedCards, "OnSelect");
        return true;
    }
}
