using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class CompoundInterest : YuWanCardModel
{
    public CompoundInterest() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<CompoundInterestPower>("InterestCap", 5);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["InterestCap"].BaseValue = 10;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CompoundInterestPower>(Owner.Creature, DynamicVars["InterestCap"].BaseValue, Owner.Creature, this);
    }
}
