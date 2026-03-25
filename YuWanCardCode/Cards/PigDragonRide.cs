using BaseLib.Utils;
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
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy)
    {
        WithDamage(7);
        WithKeywords(CardKeyword.Exhaust);
    }

    public override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        RemoveKeyword(CardKeyword.Exhaust);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay, hitCount: 3).Execute(choiceContext);
    }
}
