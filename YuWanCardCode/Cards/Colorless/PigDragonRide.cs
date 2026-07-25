using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigDragonRide : YuWanCardModel
{
    public PigDragonRide() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithDamage(7, 2);
        WithVar("HitCount", 3);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay, hitCount: DynamicVars["HitCount"].IntValue).Execute(choiceContext);
    }
}
