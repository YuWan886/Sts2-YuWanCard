using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Powers;

namespace YuWanCard.Utils;

public static class CardUtils
{
    private static readonly string[] DamageVarNames = ["Damage", "CalculatedDamage", "OstyDamage", "ExtraDamage"];
    private static readonly HashSet<CardRarity> ExcludedPoolRarities =
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

    private static List<CardModel>? _foodPigCards;
    private static List<CardModel> FoodPigCards
    {
        get
        {
            if (_foodPigCards == null)
            {
                _foodPigCards = ModelDb.AllCards
                    .Where(c => c.Tags.Contains(YuWanTags.FoodPig))
                    .ToList();
            }
            return _foodPigCards;
        }
    }

    public static CardModel GetRandomFoodPigCardCanonical(Player player)
    {
        return DeterministicRandomUtils.PickStableRandom(FoodPigCards, player.RunState.Rng.CombatCardGeneration)
               ?? FoodPigCards.First();
    }

    public static CardModel CreateRandomFoodPigCard(Player player)
    {
        CardModel canonicalCard = GetRandomFoodPigCardCanonical(player);
        var combatState = player.Creature?.CombatState;
        if (combatState != null)
        {
            return combatState.CreateCard(canonicalCard, player);
        }

        return player.RunState.CreateCard(canonicalCard, player);
    }

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
                if (ExcludedPoolRarities.Contains(card.Rarity)) continue;
                if (cardTypes == null || cardTypes.Contains(card.Type))
                {
                    allCards.Add(card);
                }
            }
        }

        return allCards;
    }

    public static string GetFoodPigIdentity(CardModel card)
    {
        return card.Id?.Entry ?? card.GetType().Name;
    }

    public static async Task RecordFoodPigPlayed(CardModel card)
    {
        if (!card.Tags.Contains(YuWanTags.FoodPig))
        {
            return;
        }

        var ownerCreature = card.Owner?.Creature;
        if (ownerCreature == null)
        {
            return;
        }

        var tracker = ownerCreature.GetPower<FoodPigTrackerPower>();
        if (tracker == null)
        {
            await PowerCmd.Apply<FoodPigTrackerPower>(new ThrowingPlayerChoiceContext(), ownerCreature, 1, ownerCreature, card);
            tracker = ownerCreature.GetPower<FoodPigTrackerPower>();
        }

        tracker?.RecordPlayedFood(card);
    }

    public static bool HasPlayedFoodPigThisTurn(Player player)
    {
        return player.Creature.GetPower<FoodPigTrackerPower>()?.HasPlayedFoodThisTurn == true;
    }

    public static int GetDistinctFoodPigPlayedThisCombat(Player player)
    {
        return player.Creature.GetPower<FoodPigTrackerPower>()?.DistinctFoodPlayedCount ?? 0;
    }

    public static List<CardModel> GetTransformableUnlockedCardsByType(Player player, CardType type, string? excludeCardEntry = null)
    {
        var types = new HashSet<CardType> { type };
        return GetAllUnlockedCards(player, types)
            .Where(card => card.IsTransformable && card.Id?.Entry != excludeCardEntry)
            .ToList();
    }

    public static CardModel? CreateRandomTransformCard(CardModel selected, Player player, bool upgradeResult = false)
    {
        var candidates = GetTransformableUnlockedCardsByType(player, selected.Type, selected.Id?.Entry);
        if (candidates.Count == 0)
        {
            return null;
        }

        CardModel replacement = CardFactory.CreateRandomCardForTransform(
            selected,
            candidates,
            isInCombat: true,
            player.RunState.Rng.CombatCardGeneration);

        if (upgradeResult && replacement.IsUpgradable)
        {
            CardCmd.Upgrade(replacement);
        }

        return replacement;
    }

    public static bool HasDamageVariable(CardModel? card)
    {
        if (card == null)
        {
            return false;
        }

        var vars = card.DynamicVars;
        foreach (var varName in DamageVarNames)
        {
            if (vars.ContainsKey(varName))
            {
                return true;
            }
        }

        return false;
    }
}
