using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(StatusCardPool))]
public sealed class FerrousWroughtnautShift : YuWanCardModel
{
    public override int MaxUpgradeLevel => 0;

    public FerrousWroughtnautShift() : base(
        baseCost: 1,
        type: CardType.Status,
        rarity: CardRarity.Status,
        target: TargetType.None)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        FerrousWroughtnautPositioning.Toggle(Owner.Creature);
        return Task.CompletedTask;
    }
}
