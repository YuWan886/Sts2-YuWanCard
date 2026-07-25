using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public sealed class PigEatInterest : YuWanCardModel
{
    public PigEatInterest() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<PigInterestPower>(5, 3);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PigInterestPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["PigInterestPower"].BaseValue,
            Owner.Creature,
            this);
    }
}
