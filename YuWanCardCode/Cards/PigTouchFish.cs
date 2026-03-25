using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigTouchFish : YuWanCardModel
{
    public PigTouchFish() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<RetainHandPower>(1);
        WithPower<EnergyNextTurnPower>(2);
        WithTip(CardKeyword.Retain);
        WithEnergyTip();
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RetainHandPower>(Owner.Creature, DynamicVars["RetainHandPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, DynamicVars["EnergyNextTurnPower"].BaseValue, Owner.Creature, this);
    }
}
