using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Config;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class ColorlessCardPoolAvailabilityPatch
{
    [HarmonyPostfix]
    private static IEnumerable<CardModel> Postfix(CardPoolModel __instance, IEnumerable<CardModel> __result)
    {
        if (!__instance.IsColorless)
        {
            return __result;
        }

        return YuWanContentAvailability.FilterAvailableCards(__result);
    }
}

[HarmonyPatch(typeof(CardFactory), "GetFilteredTransformationOptions")]
internal static class ColorlessTransformCandidateAvailabilityPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref IEnumerable<CardModel> originalOptions)
    {
        originalOptions = YuWanContentAvailability.FilterAvailableCards(originalOptions);
    }
}

[HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.GetPossibleCards))]
internal static class ColorlessRewardSourceAvailabilityPatch
{
    [HarmonyPostfix]
    private static IEnumerable<CardModel> Postfix(IEnumerable<CardModel> __result)
    {
        return YuWanContentAvailability.FilterAvailableCards(__result);
    }
}

[HarmonyPatch(typeof(CardFactory))]
internal static class ColorlessCombatGenerationAvailabilityPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardFactory.FilterForCombat))]
    private static IEnumerable<CardModel> FilterForCombatPostfix(IEnumerable<CardModel> __result)
    {
        return YuWanContentAvailability.FilterAvailableCards(__result);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType))]
    [HarmonyPatch(nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity))]
    private static void CreateForMerchantPrefix(ref IEnumerable<CardModel> options)
    {
        options = YuWanContentAvailability.FilterAvailableCards(options);
    }
}

[HarmonyPatch(typeof(CardReward), nameof(CardReward.Populate))]
internal static class ColorlessCardRewardAvailabilityPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardReward __instance)
    {
        var cardsField = AccessTools.Field(typeof(CardReward), "_cards");
        if (cardsField.GetValue(__instance) is not List<CardCreationResult> cards)
        {
            return;
        }

        cards.RemoveAll(static result => !YuWanContentAvailability.IsCardEnabled(result.Card));
    }
}
