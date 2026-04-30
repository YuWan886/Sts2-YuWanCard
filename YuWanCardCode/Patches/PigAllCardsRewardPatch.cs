using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CardFactory))]
public class PigAllCardsRewardPatch
{
    private static readonly MethodInfo RollForRarityMethod = AccessTools.Method(typeof(CardFactory), "RollForRarity");
    private static readonly MethodInfo FilterForPlayerCountMethod = AccessTools.Method(typeof(CardFactory), "FilterForPlayerCount");

    [HarmonyPrefix]
    [HarmonyPatch("CreateForReward", [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardCreationOptions)])]
    public static bool Prefix(Player player, IEnumerable<CardModel> blacklist, CardCreationOptions options, ref CardModel __result)
    {
        if (player.Character is not Pig)
            return true;

        bool hasPigAllCards = player.RunState.Modifiers.Any(m => m is PigAllCards);
        if (!hasPigAllCards)
            return true;

        options = Hook.ModifyCardRewardCreationOptions(player.RunState, player, options);
        IEnumerable<CardModel> possibleCards = options.GetPossibleCards(player).Except(blacklist).ToList();
        possibleCards = ((IEnumerable<CardModel>)FilterForPlayerCountMethod.Invoke(null, [player.RunState, possibleCards])!).ToList();

        var pigCards = possibleCards.Where(c => c.Pool is PigCardPool).ToList();
        var otherCards = possibleCards.Where(c => c.Pool is not PigCardPool).ToList();

        Rng rng = options.RngOverride ?? player.PlayerRng.Rewards;
        bool usePigPool = rng.NextFloat() < 0.25f;
        var primaryPool = usePigPool ? pigCards : otherCards;
        var fallbackPool = usePigPool ? otherCards : pigCards;

        CardRarity? selectedRarity = null;
        IEnumerable<CardModel> items;

        if (options.RarityOdds == CardRarityOddsType.Uniform)
        {
            items = primaryPool.Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient);
            if (!items.Any())
            {
                items = fallbackPool.Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient);
            }
        }
        else
        {
            // OrderBy before ToHashSet ensures deterministic insertion order for consistent iteration
            var allRarities = possibleCards.Select(c => c.Rarity).Distinct().OrderBy(r => (int)r).ToHashSet();
            selectedRarity = (CardRarity?)RollForRarityMethod.Invoke(
                null,
                [player, options.RarityOdds, options.Source, allRarities, options.Flags.HasFlag(CardCreationFlags.ForceRarityOddsChange)]
            );

            if (selectedRarity == null || selectedRarity == CardRarity.None)
            {
                throw new InvalidOperationException($"Tried to create a card for a reward, but we couldn't generate a valid rarity! Odds: {options.RarityOdds} Card pool: {string.Join(",", possibleCards)}, blacklist: {string.Join(",", blacklist)}");
            }

            var rarity = selectedRarity.Value;
            items = primaryPool.Where(c => c.Rarity == rarity);
            if (!items.Any())
            {
                items = fallbackPool.Where(c => c.Rarity == rarity);
            }
        }

        CardModel? cardModel = rng.NextItem(items);
        if (cardModel == null)
        {
            throw new InvalidOperationException($"Tried to create a card for a reward, but we couldn't generate a valid card! Selected rarity: {selectedRarity}, card pool: {string.Join(",", primaryPool)}, blacklist: {string.Join(",", blacklist)}, odds: {options.RarityOdds}");
        }

        __result = player.RunState.CreateCard(cardModel, player);
        return false;
    }
}
