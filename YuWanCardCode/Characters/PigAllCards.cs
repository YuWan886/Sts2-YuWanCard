using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.Characters;

public class PigAllCards : YuWanModifierModel
{
    public override LocString Title => new("modifiers", "YUWANCARD-PIG_ALL_CARDS.title");
    public override LocString Description => new("modifiers", "YUWANCARD-PIG_ALL_CARDS.description");

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        return () => Task.CompletedTask;
    }

    public static HashSet<CardModel> GetAllUnlockedCards(Player player)
    {
        var allCards = new HashSet<CardModel>();
        
        foreach (var pool in ModelDb.AllCardPools)
        {
            foreach (var card in pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            {
                allCards.Add(card);
            }
        }

        return allCards;
    }

    public static HashSet<CardModel> GetAllUnlockedCardsByType(Player player, CardType cardType)
    {
        var allCards = new HashSet<CardModel>();
        
        foreach (var pool in ModelDb.AllCardPools)
        {
            foreach (var card in pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            {
                if (card.Type == cardType)
                {
                    allCards.Add(card);
                }
            }
        }

        return allCards;
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
        {
            return options;
        }

        if (player.Character is not Pig)
        {
            return options;
        }

        var allCards = new HashSet<CardModel>(options.GetPossibleCards(player));
        allCards.UnionWith(GetAllUnlockedCards(player));

        return options.WithCustomPool(allCards);
    }

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> options)
    {
        if (player.Character is not Pig)
        {
            return options;
        }

        var allCards = new HashSet<CardModel>(options);
        allCards.UnionWith(GetAllUnlockedCards(player));

        return allCards;
    }
}
