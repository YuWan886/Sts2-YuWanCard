using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigMultiShot : YuWanCardModel
{
    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar("Repeat", 3m)
    ];

    public PigMultiShot() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Token,
        target: TargetType.AnyEnemy
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitCount(DynamicVars["Repeat"].IntValue)
            .Execute(choiceContext);
    }

    public override void OnUpgrade()
    {
        DynamicVars["Repeat"].UpgradeValueBy(2m);
    }
}
