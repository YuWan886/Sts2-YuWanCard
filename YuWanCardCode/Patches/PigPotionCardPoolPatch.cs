using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

public static class PigPotionHelper
{
    public static IEnumerable<CardModel> GetCardsForPotion(PotionModel potion, CardType cardType)
    {
        var player = potion.Owner;
        if (player.Character is Pig)
        {
            return PigCardPoolUtils.GetAllUnlockedCards(player, [cardType]);
        }
        
        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == cardType);
    }
}

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat))]
public static class CardFactoryGetDistinctForCombatPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player, IEnumerable<CardModel> cards, int count, Rng rng, ref IEnumerable<CardModel> __result)
    {
        var cardList = cards.Where(c => c != null).ToList();
        
        if (player.Character is Pig)
        {
            var originalTypes = cardList.Select(c => c.Type).Distinct().ToHashSet();
            var isColorless = cardList.Any(c => c.Pool is ColorlessCardPool);
            
            var allCards = new HashSet<CardModel>(cardList);
            
            if (originalTypes.Count > 0)
            {
                var unlockedCards = PigCardPoolUtils.GetAllUnlockedCards(player, originalTypes, colorlessOnly: isColorless).Where(c => c != null);
                allCards.UnionWith(unlockedCards);
            }
            else
            {
                var unlockedCards = PigCardPoolUtils.GetAllUnlockedCards(player, colorlessOnly: isColorless).Where(c => c != null);
                allCards.UnionWith(unlockedCards);
            }
            
            cardList = [.. allCards];
            
            MainFile.Logger.Debug($"GetDistinctForCombat: Pig character detected, types: [{string.Join(", ", originalTypes)}], colorless: {isColorless}, total cards: {cardList.Count}");
        }
        
        if (cardList.Count == 0)
        {
            MainFile.Logger.Warn("GetDistinctForCombat: No cards available");
            __result = [];
            return false;
        }

        var filtered = FilterForCombatWithBasicFallback(cardList).ToList();
        
        if (filtered.Count == 0)
        {
            MainFile.Logger.Warn("GetDistinctForCombat: No cards after filtering");
            __result = [];
            return false;
        }

        filtered = [.. CardFactory.FilterForPlayerCount(player.RunState, filtered)];
        var selected = filtered.TakeRandom(count, rng);
        __result = selected.Select(c => player.Creature!.CombatState!.CreateCard(c, player));
        
        MainFile.Logger.Debug($"GetDistinctForCombat: Selected {selected.Count()} cards");
        
        return false;
    }

    private static IEnumerable<CardModel> FilterForCombatWithBasicFallback(IEnumerable<CardModel> cards)
    {
        var cardList = cards.Where(c => c != null).ToList();
        var filtered = cardList.Where(c => c.CanBeGeneratedInCombat && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient && c.Rarity != CardRarity.Event).Distinct().ToList();
        
        if (filtered.Count > 0)
        {
            return filtered;
        }

        var basicCards = cardList.Where(c => c.CanBeGeneratedInCombat && c.Rarity == CardRarity.Basic).Distinct().ToList();
        if (basicCards.Count > 0)
        {
            return basicCards;
        }

        return cardList.Where(c => c.CanBeGeneratedInCombat).Distinct();
    }
}
