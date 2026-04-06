using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

public static class PigPotionHelper
{
    private static readonly System.Threading.AsyncLocal<CardType?> CurrentPotionCardType = new();
    
    public static CardType? GetAndClearPotionCardType()
    {
        var type = CurrentPotionCardType.Value;
        CurrentPotionCardType.Value = null;
        return type;
    }
    
    public static void SetPotionCardType(CardType cardType)
    {
        CurrentPotionCardType.Value = cardType;
    }
    
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

[HarmonyPatch(typeof(PowerPotion))]
public static class PowerPotionPatch
{
    [HarmonyPatch("OnUse")]
    [HarmonyPrefix]
    public static void OnUsePrefix(PowerPotion __instance)
    {
        if (__instance.Owner?.Character is Pig)
        {
            PigPotionHelper.SetPotionCardType(CardType.Power);
        }
    }
}

[HarmonyPatch(typeof(AttackPotion))]
public static class AttackPotionPatch
{
    [HarmonyPatch("OnUse")]
    [HarmonyPrefix]
    public static void OnUsePrefix(AttackPotion __instance)
    {
        if (__instance.Owner?.Character is Pig)
        {
            PigPotionHelper.SetPotionCardType(CardType.Attack);
        }
    }
}

[HarmonyPatch(typeof(SkillPotion))]
public static class SkillPotionPatch
{
    [HarmonyPatch("OnUse")]
    [HarmonyPrefix]
    public static void OnUsePrefix(SkillPotion __instance)
    {
        if (__instance.Owner?.Character is Pig)
        {
            PigPotionHelper.SetPotionCardType(CardType.Skill);
        }
    }
}

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat))]
public static class CardFactoryGetDistinctForCombatPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player, IEnumerable<CardModel> cards, int count, Rng rng, ref IEnumerable<CardModel> __result)
    {
        var potionCardType = PigPotionHelper.GetAndClearPotionCardType();
        
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
            else if (potionCardType.HasValue)
            {
                var unlockedCards = PigCardPoolUtils.GetAllUnlockedCards(player, [potionCardType.Value], colorlessOnly: isColorless).Where(c => c != null);
                allCards.UnionWith(unlockedCards);
                MainFile.Logger.Debug($"GetDistinctForCombat: Using potion card type {potionCardType.Value}, found {allCards.Count} cards");
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

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetForCombat))]
public static class CardFactoryGetForCombatPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player, IEnumerable<CardModel> cards, int count, Rng rng, ref IEnumerable<CardModel> __result)
    {
        var cardList = cards.Where(c => c != null).ToList();
        
        if (player.Character is Pig)
        {
            var originalTypes = cardList.Select(c => c.Type).Distinct().ToHashSet();
            var isColorless = cardList.Any(c => c.Pool is ColorlessCardPool);
            bool wantsZeroCost = cardList.Count > 0 && cardList.All(c => c.EnergyCost?.Canonical == 0 && !c.EnergyCost.CostsX);
            
            var allCards = new HashSet<CardModel>(cardList);
            
            if (cardList.Count == 0)
            {
                var unlockedCards = PigCardPoolUtils.GetAllUnlockedCards(player, colorlessOnly: isColorless).Where(c => c != null);
                allCards.UnionWith(unlockedCards);
                wantsZeroCost = true;
                MainFile.Logger.Debug($"GetForCombat: Pig character got empty card list, fetching from all card pools, found {allCards.Count} cards");
            }
            else if (originalTypes.Count > 0)
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
            
            if (wantsZeroCost)
            {
                cardList = cardList.Where(c => c.EnergyCost != null && c.EnergyCost.Canonical == 0 && !c.EnergyCost.CostsX).ToList();
                MainFile.Logger.Debug($"GetForCombat: Filtering for zero-cost cards, found {cardList.Count} cards");
            }
            
            MainFile.Logger.Debug($"GetForCombat: Pig character detected, types: [{string.Join(", ", originalTypes)}], colorless: {isColorless}, total cards: {cardList.Count}");
        }
        
        if (cardList.Count == 0)
        {
            MainFile.Logger.Warn("GetForCombat: No cards available");
            __result = [];
            return false;
        }

        var filtered = FilterForCombatWithBasicFallback(cardList).ToList();
        
        if (filtered.Count == 0)
        {
            MainFile.Logger.Warn("GetForCombat: No cards after filtering");
            __result = [];
            return false;
        }

        filtered = [.. CardFactory.FilterForPlayerCount(player.RunState, filtered)];
        
        var result = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            var canonicalCard = rng.NextItem(filtered);
            if (canonicalCard == null)
            {
                MainFile.Logger.Warn($"GetForCombat: rng.NextItem returned null at iteration {i}");
                continue;
            }
            var item = player.Creature!.CombatState!.CreateCard(canonicalCard, player);
            result.Add(item);
        }
        
        __result = result;
        MainFile.Logger.Debug($"GetForCombat: Created {result.Count} cards");
        
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
