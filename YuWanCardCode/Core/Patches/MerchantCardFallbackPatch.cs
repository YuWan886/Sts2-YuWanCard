using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(CardFactory))]
public static class MerchantCardFallbackPatch
{
    [HarmonyPatch(nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType))]
    [HarmonyPrefix]
    public static bool CreateForMerchantByTypePrefix(Player player, IEnumerable<CardModel> options, CardType type, ref CardCreationResult __result)
    {
        if (player.Character is Deprived)
        {
            return true;
        }

        var filteredOptions = PrepareMerchantOptions(player, options).ToArray();
        var rolledRarity = Hook.ModifyMerchantCardRarity(
            player.RunState,
            player,
            player.PlayerOdds.CardRarity.RollWithoutChangingFutureOdds(CardRarityOddsType.Shop));

        var selectedCard = TryPickMerchantCard(player, filteredOptions, type, rolledRarity);
        if (selectedCard == null)
        {
            return true;
        }

        __result = CreateMerchantCardResult(player, selectedCard);
        return false;
    }

    [HarmonyPatch(nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity))]
    [HarmonyPrefix]
    public static bool CreateForMerchantByRarityPrefix(Player player, IEnumerable<CardModel> options, CardRarity rarity, ref CardCreationResult __result)
    {
        var filteredOptions = PrepareMerchantOptions(player, options).ToArray();
        var modifiedRarity = Hook.ModifyMerchantCardRarity(player.RunState, player, rarity);

        var fallbackRarities = GetFallbackRarities(modifiedRarity);
        var selectedPool = fallbackRarities
            .SelectMany(r => filteredOptions.Where(c => c.Rarity == r))
            .ToList();

        if (selectedPool.Count == 0)
        {
            return true;
        }

        var selectedCard = player.PlayerRng.Shops.NextItem(selectedPool);
        if (selectedCard == null)
        {
            return true;
        }

        __result = CreateMerchantCardResult(player, selectedCard);
        return false;
    }

    private static IEnumerable<CardModel> PrepareMerchantOptions(Player player, IEnumerable<CardModel> options)
    {
        var filtered = Hook.ModifyMerchantCardPool(player.RunState, player, options)
            .Where(c => c.Rarity != CardRarity.Basic);

        if (player.RunState.Players.Count > 1)
        {
            return filtered.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly);
        }

        return filtered.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
    }

    private static CardModel? TryPickMerchantCard(Player player, IEnumerable<CardModel> options, CardType type, CardRarity rolledRarity)
    {
        foreach (var rarity in GetFallbackRarities(rolledRarity))
        {
            var matches = options
                .Where(c => c.Rarity == rarity && c.Type == type)
                .ToList();

            if (matches.Count > 0)
            {
                return player.PlayerRng.Shops.NextItem(matches);
            }
        }

        return null;
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

    private static CardCreationResult CreateMerchantCardResult(Player player, CardModel selectedCard)
    {
        var createdCard = player.RunState.CreateCard(selectedCard, player);

        // Preserve the base game's reward RNG consumption for merchant card creation.
        player.PlayerRng.Rewards.NextFloat();

        return new CardCreationResult(createdCard);
    }
}
