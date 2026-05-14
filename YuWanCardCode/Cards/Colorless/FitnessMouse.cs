using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class FitnessMouse : YuWanCardModel
{
    public FitnessMouse() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVar("Strength", 3);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<FitnessMouseNextTurnStrengthPower>()));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FitnessMouseNextTurnStrengthPower>(
            Owner.Creature,
            DynamicVars["Strength"].IntValue,
            Owner.Creature,
            this);
    }
}
