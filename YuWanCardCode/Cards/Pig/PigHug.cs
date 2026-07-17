using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigHug : YuWanCardModel
{
    public PigHug() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: CustomTargetType.AnyFriendly)
    {
        WithBlock(7, 3);
        WithPower<RegenPower>(2, 1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { IsDead: false } target)
        {
            return;
        }

        await CreatureCmd.GainBlock(target, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<RegenPower>(new ThrowingPlayerChoiceContext(), target, DynamicVars["RegenPower"].IntValue, Owner.Creature, this);
    }
}
