using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Utils;

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
        WithGold(8, 6);
        WithEnergy(1, 1);
    }


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int goldCost = DynamicVars.Gold.IntValue;
        if (!await GoldSpendHelper.TrySpend(Owner, goldCost, nameof(PigPayment)))
        {
            return;
        }
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
