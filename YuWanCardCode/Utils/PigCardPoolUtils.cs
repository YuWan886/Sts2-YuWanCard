using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

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
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)) return options;

        var originalCards = options.GetPossibleCards(player).ToList();
        if (originalCards.Count == 0) return options;

        var originalTypes = originalCards.Select(c => c.Type).Distinct().ToHashSet();
        var originalRarities = originalCards.Select(c => c.Rarity).Distinct().ToHashSet();
        
        bool isColorless = options.CardPools.Any(p => p.IsColorless);
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
            allCards = GetAllUnlockedCards(player, originalTypes, colorlessOnly: isColorless);
        }

        if (allCards.Count == 0) return options;

        var distinctRarities = allCards.Select(c => c.Rarity).Distinct().ToList();
        if (distinctRarities.Count == 1)
        {
            return options.WithCustomPool(allCards, CardRarityOddsType.Uniform);
        }

        return options.WithCustomPool(allCards);
    }
}
