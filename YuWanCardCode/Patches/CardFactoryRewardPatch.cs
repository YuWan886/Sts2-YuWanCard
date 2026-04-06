using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CardFactory))]
public static class CardFactoryRewardPatch
{
    [HarmonyPatch("CreateForReward", [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardCreationOptions)])]
    [HarmonyPrefix]
    public static bool CreateForRewardPrefix(Player player, IEnumerable<CardModel> blacklist, CardCreationOptions options, ref CardModel __result)
    {
        if (player.Character is not Pig) return true;
        
        options = Hook.ModifyCardRewardCreationOptions(player.RunState, player, options);
        
        var possibleCards = options.GetPossibleCards(player).Except(blacklist).ToList();
        possibleCards = CardFactory.FilterForPlayerCount(player.RunState, possibleCards).ToList();
        
        if (possibleCards.Count == 0)
        {
            MainFile.Logger.Warn("CreateForReward: No cards available after filtering, using all unlocked cards");
            possibleCards = PigCardPoolUtils.GetAllUnlockedCards(player).ToList();
            possibleCards = CardFactory.FilterForPlayerCount(player.RunState, possibleCards).ToList();
        }
        
        if (possibleCards.Count == 0)
        {
            MainFile.Logger.Error("CreateForReward: Still no cards available!");
            __result = null!;
            return false;
        }
        
        IEnumerable<CardModel> items;
        if (options.RarityOdds == CardRarityOddsType.Uniform)
        {
            items = possibleCards.Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient).ToList();
            
            if (!items.Any())
            {
                MainFile.Logger.Debug("CreateForReward: No non-Basic/Ancient cards, using all available cards");
                items = possibleCards;
            }
        }
        else
        {
            var allowedRarities = possibleCards.Select(c => c.Rarity).ToHashSet();
            var selectedRarity = RollForRarity(player, options, allowedRarities);
            
            if (selectedRarity == CardRarity.None)
            {
                MainFile.Logger.Warn($"CreateForReward: Could not roll rarity, using random card from pool");
                items = possibleCards;
            }
            else
            {
                items = possibleCards.Where(c => c.Rarity == selectedRarity).ToList();
                
                if (!items.Any())
                {
                    items = possibleCards;
                }
            }
        }
        
        var rng = options.RngOverride ?? player.PlayerRng.Rewards;
        var cardModel = rng.NextItem(items.ToList());
        
        if (cardModel == null)
        {
            MainFile.Logger.Error("CreateForReward: Failed to select a card");
            __result = null!;
            return false;
        }
        
        __result = player.RunState.CreateCard(cardModel, player);
        MainFile.Logger.Debug($"CreateForReward: Created card {cardModel.Id} for Pig character");
        
        return false;
    }
    
    private static CardRarity RollForRarity(Player player, CardCreationOptions options, HashSet<CardRarity> allowedRarities)
    {
        var rarityOdds = options.RarityOdds;
        
        float uncommonChance, rareChance;
        
        switch (rarityOdds)
        {
            case CardRarityOddsType.RegularEncounter:
                uncommonChance = 0.37f;
                rareChance = 0.13f;
                break;
            case CardRarityOddsType.EliteEncounter:
                uncommonChance = 0.5f;
                rareChance = 0.15f;
                break;
            case CardRarityOddsType.BossEncounter:
                uncommonChance = 0.5f;
                rareChance = 0.5f;
                break;
            default:
                return CardRarity.None;
        }
        
        var rng = options.RngOverride ?? player.PlayerRng.Rewards;
        var roll = rng.NextFloat();
        
        if (allowedRarities.Contains(CardRarity.Rare) && roll < rareChance)
            return CardRarity.Rare;
        if (allowedRarities.Contains(CardRarity.Uncommon) && roll < rareChance + uncommonChance)
            return CardRarity.Uncommon;
        if (allowedRarities.Contains(CardRarity.Common))
            return CardRarity.Common;
        
        if (allowedRarities.Contains(CardRarity.Uncommon))
            return CardRarity.Uncommon;
        if (allowedRarities.Contains(CardRarity.Rare))
            return CardRarity.Rare;
        
        return CardRarity.None;
    }
}
