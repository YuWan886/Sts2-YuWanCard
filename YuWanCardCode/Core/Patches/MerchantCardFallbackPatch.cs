using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(
    typeof(CardFactory),
    nameof(CardFactory.CreateForMerchant),
    [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity)])]
public static class MerchantCardFallbackPatch
{
    private static readonly Action<Player, CardModel, decimal> RollForUpgrade =
        AccessTools.MethodDelegate<Action<Player, CardModel, decimal>>(
            AccessTools.Method(
                typeof(CardFactory),
                "RollForUpgrade",
                [typeof(Player), typeof(CardModel), typeof(decimal)]));

    [HarmonyPrefix]
    public static bool CreateForMerchantByRarityPrefix(
        Player player,
        IEnumerable<CardModel> options,
        CardRarity rarity,
        ref CardCreationResult __result)
    {
        CardModel[] filteredOptions = PrepareMerchantOptions(player, options).ToArray();
        CardRarity modifiedRarity = Hook.ModifyMerchantCardRarity(player.RunState, player, rarity);
        List<CardModel> selectedPool = GetFallbackRarities(modifiedRarity)
            .Select(candidate => filteredOptions.Where(card => card.Rarity == candidate).ToList())
            .FirstOrDefault(matches => matches.Count > 0)
            ?? [];

        CardModel selectedCard = player.PlayerRng.Shops.NextItem(selectedPool)
            ?? throw new InvalidOperationException(
                $"Can't generate a merchant card for rarity {modifiedRarity} from the supplied options.");
        CardModel createdCard = player.RunState.CreateCard(selectedCard, player);
        RollForUpgrade(player, createdCard, -999999999m);
        __result = new CardCreationResult(createdCard);
        return false;
    }

    private static IEnumerable<CardModel> PrepareMerchantOptions(
        Player player,
        IEnumerable<CardModel> options)
    {
        IEnumerable<CardModel> filtered = Hook.ModifyMerchantCardPool(player.RunState, player, options)
            .Where(card => card.Rarity != CardRarity.Basic);

        return player.RunState.Players.Count > 1
            ? filtered.Where(card => card.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly)
            : filtered.Where(card => card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
    }

    private static IReadOnlyList<CardRarity> GetFallbackRarities(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => [CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare],
            CardRarity.Uncommon => [CardRarity.Uncommon, CardRarity.Rare, CardRarity.Common],
            CardRarity.Rare => [CardRarity.Rare, CardRarity.Uncommon, CardRarity.Common],
            _ => [rarity, CardRarity.Uncommon, CardRarity.Common, CardRarity.Rare]
        };
    }
}
