using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Inflation : YuWanCardModel
{
    public Inflation() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<InflationPower>("InflationBonus", 50, 25);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InflationPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["InflationBonus"].BaseValue, Owner.Creature, this);
    }
}
