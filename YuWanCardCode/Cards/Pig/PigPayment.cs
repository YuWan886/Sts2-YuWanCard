using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPayment : YuWanCardModel
{
    public PigPayment() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithVars(new GoldVar(20), new EnergyVar(1));
        WithEnergyTip();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Gold.UpgradeValueBy(10);
        DynamicVars.Energy.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int goldCost = DynamicVars.Gold.IntValue;
        if (Owner.Gold < goldCost)
        {
            MainFile.Logger.Warn($"PigPayment: Not enough gold ({Owner.Gold} < {goldCost})");
            return;
        }

        await PlayerCmd.LoseGold(goldCost, Owner, GoldLossType.Spent);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
