using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class EndlessBuffetRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner
            || !cardPlay.Card.Tags.Contains(YuWanTags.FoodPig)
            || Owner?.PlayerCombatState?.Hand == null)
        {
            return Task.CompletedTask;
        }

        if (!HextechPigRuneSharedState.RollPercent(this, 0.5f, 0.25f))
        {
            return Task.CompletedTask;
        }

        Flash();
        CardModel copy = Owner.RunState.CreateCard(cardPlay.Card.CanonicalInstance ?? cardPlay.Card, Owner);
        return CardPileCmd.AddGeneratedCardsToCombat([copy], PileType.Hand, Owner);
    }
}
