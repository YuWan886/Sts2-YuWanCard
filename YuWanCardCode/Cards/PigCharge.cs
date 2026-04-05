using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigCharge : YuWanCardModel
{
    public PigCharge() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Token,
        target: TargetType.AnyEnemy)
    {
        WithDamage(9);
        WithPower<StrengthPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay, hitCount: 1).Execute(choiceContext);
        await PowerCmd.Apply<PigChargePower>(Owner.Creature, DynamicVars.Strength.IntValue, Owner.Creature, this);
    }
}
