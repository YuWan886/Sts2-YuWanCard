using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Monsters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPat : YuWanCardModel
{
    public PigPat() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: CustomTargetType.AnyFriendly)
    {
        WithHeal(3, 2);
        WithCards(1, 1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { IsDead: false } target)
        {
            return;
        }

        await CreatureCmd.Heal(target, DynamicVars.Heal.BaseValue);

        if (target.Monster is not PigMinion || Owner.Creature.HasPower<PigPatTrackerPower>())
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PowerCmd.Apply<PigPatTrackerPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
    }
}
