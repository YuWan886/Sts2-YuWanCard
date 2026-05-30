using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(EventCardPool))]
public class Horizon : YuWanCardModel
{
    public Horizon() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Ancient,
        target: TargetType.Self)
    {
        WithPower<HorizonPower>(3);
        WithKeywords(CardKeyword.Innate);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HorizonPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["HorizonPower"].BaseValue, Owner.Creature, this);
    }
}
