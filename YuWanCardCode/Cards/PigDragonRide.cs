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
public class PigDragonRide : YuWanCardModel
{
    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public PigDragonRide() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitCount(3)
            .Execute(choiceContext);
    }

    public override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
