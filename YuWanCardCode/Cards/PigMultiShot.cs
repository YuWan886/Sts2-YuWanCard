using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigMultiShot : YuWanCardModel
{
    public PigMultiShot() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Token,
        target: TargetType.AnyEnemy)
    {
        WithDamage(3);
        WithVar("Repeat", 3);
    }

    public override void OnUpgrade()
    {
        DynamicVars["Repeat"].UpgradeValueBy(2m);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay, hitCount: DynamicVars["Repeat"].IntValue).Execute(choiceContext);
    }
}
