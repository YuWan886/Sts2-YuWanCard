using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;

namespace YuWanCard.Utils;

public static class PigCardPoolUtils
{
    private static readonly HashSet<CardRarity> ExcludedRarities = 
    [
        CardRarity.None,
        CardRarity.Basic,
        CardRarity.Ancient,
        CardRarity.Event,
        CardRarity.Token,
        CardRarity.Status,
        CardRarity.Curse,
        CardRarity.Quest
    ];

    public static HashSet<CardModel> GetAllUnlockedCards(Player player, HashSet<CardType>? cardTypes = null, bool colorlessOnly = false)
    {
        var allCards = new HashSet<CardModel>();
        
        foreach (var pool in ModelDb.AllCardPools)
        {
            if (pool == null) continue;
            if (colorlessOnly && !pool.IsColorless) continue;
            
            foreach (var card in pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            {
                if (card == null) continue;
                if (ExcludedRarities.Contains(card.Rarity)) continue;
                if (cardTypes == null || cardTypes.Contains(card.Type))
                {
                    allCards.Add(card);
                }
            }
        }

        return allCards;
    }

    public static HashSet<CardModel> GetAllCardsByTypesAndRarities(Player player, HashSet<CardType> types, HashSet<CardRarity> rarities)
    {
        var allCards = new HashSet<CardModel>();
        
        foreach (var pool in ModelDb.AllCardPools)
        {
            if (pool == null) continue;
            
            foreach (var card in pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            {
                if (card == null) continue;
                if (ExcludedRarities.Contains(card.Rarity)) continue;
                if (types.Contains(card.Type) && rarities.Contains(card.Rarity))
                {
                    allCards.Add(card);
                }
            }
        }

        return allCards;
    }

    public static CardCreationOptions ModifyCardRewardOptions(Player player, CardCreationOptions options)
    {
        if (options.Source != CardCreationSource.Encounter) return options;
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)) return options;

        var originalCards = options.GetPossibleCards(player).ToList();
        if (originalCards.Count == 0) return options;

        var originalTypes = originalCards.Select(c => c.Type).Distinct().ToHashSet();
        var originalRarities = originalCards.Select(c => c.Rarity).Distinct().ToHashSet();
        bool preserveRarity = options.Flags.HasFlag(CardCreationFlags.NoRarityModification);
        bool hasRarityFilter = originalRarities.Count == 1 && !ExcludedRarities.Contains(originalRarities.First());

        HashSet<CardModel> allCards;

        if (preserveRarity || hasRarityFilter)
        {
            var validRarities = originalRarities.Where(r => !ExcludedRarities.Contains(r)).ToHashSet();
            allCards = GetAllCardsByTypesAndRarities(player, originalTypes, validRarities);
        }
        else
        {
            allCards = GetAllUnlockedCards(player, originalTypes);
        }

        if (allCards.Count == 0) return options;

        var distinctRarities = allCards.Select(c => c.Rarity).Distinct().ToList();
        if (distinctRarities.Count == 1)
        {
            return options.WithCustomPool(allCards, CardRarityOddsType.Uniform);
        }

        return options.WithCustomPool(allCards);
    }

    public static bool TryNormalizePigCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (creationOptions.Source != CardCreationSource.Encounter)
        {
            return false;
        }

        if (cardRewardOptions.Count == 0)
        {
            return false;
        }

        var rewardOptions = ModifyCardRewardOptions(player, creationOptions);
        var possibleCards = rewardOptions.GetPossibleCards(player).ToList();
        var pigCards = possibleCards.Where(IsPigPoolCard).ToList();
        var otherCards = possibleCards.Where(c => !IsPigPoolCard(c)).ToList();

        if (pigCards.Count == 0 || otherCards.Count == 0)
        {
            return false;
        }

        Rng rng = creationOptions.RngOverride ?? player.PlayerRng.Rewards;
        var existingPigIndexes = cardRewardOptions
            .Select((result, index) => new { Card = GetCardModel(result), index })
            .Where(x => x.Card != null && IsPigPoolCard(x.Card))
            .Select(x => x.index)
            .ToList();

        int pigIndex = existingPigIndexes.Count > 0
            ? existingPigIndexes[rng.NextInt(existingPigIndexes.Count)]
            : rng.NextInt(cardRewardOptions.Count);

        var usedCardIds = new HashSet<string>(StringComparer.Ordinal);

        if (!TryKeepOrReplaceRewardCard(player, cardRewardOptions, pigIndex, pigCards, usedCardIds, requirePigPool: true, rng))
        {
            return false;
        }

        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            if (i == pigIndex)
            {
                continue;
            }

            if (!TryKeepOrReplaceRewardCard(player, cardRewardOptions, i, otherCards, usedCardIds, requirePigPool: false, rng))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryKeepOrReplaceRewardCard(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        int index,
        List<CardModel> candidates,
        HashSet<string> usedCardIds,
        bool requirePigPool,
        Rng rng)
    {
        var currentModel = GetCardModel(cardRewardOptions[index]);
        if (currentModel?.Id?.Entry is string currentId
            && IsPigPoolCard(currentModel) == requirePigPool
            && !usedCardIds.Contains(currentId))
        {
            usedCardIds.Add(currentId);
            return true;
        }

        var replacement = PickReplacementCard(candidates, usedCardIds, rng);
        if (replacement == null)
        {
            return false;
        }

        if (replacement.Id?.Entry is string replacementId)
        {
            usedCardIds.Add(replacementId);
        }

        cardRewardOptions[index] = new CardCreationResult(player.RunState.CreateCard(replacement, player));
        return true;
    }

    private static CardModel? PickReplacementCard(IEnumerable<CardModel> candidates, HashSet<string> usedCardIds, Rng rng)
    {
        var availableCards = candidates
            .Where(c => c.Id?.Entry != null && !usedCardIds.Contains(c.Id.Entry))
            .ToList();

        var selectionPool = availableCards.Count > 0 ? availableCards : candidates.ToList();
        if (selectionPool.Count == 0)
        {
            return null;
        }

        return selectionPool[rng.NextInt(selectionPool.Count)];
    }

    private static CardModel? GetCardModel(CardCreationResult result)
    {
        return result.Card?.CanonicalInstance ?? result.Card;
    }

    private static bool IsPigPoolCard(CardModel card)
    {
        return card.Pool is PigCardPool;
    }
}
