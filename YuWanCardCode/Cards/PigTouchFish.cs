using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigTouchFish : YuWanCardModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        HoverTipFactory.ForEnergy(this)
    ];

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RetainHandPower>(1m),
        new PowerVar<EnergyNextTurnPower>(2m)
    ];

    public PigTouchFish() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RetainHandPower>(Owner.Creature, DynamicVars["RetainHandPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, DynamicVars["EnergyNextTurnPower"].BaseValue, Owner.Creature, this);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
