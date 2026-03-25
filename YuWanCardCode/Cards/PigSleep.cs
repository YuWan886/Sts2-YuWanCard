using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigSleep : YuWanCardModel
{
    public PigSleep() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithBlock(10);
        WithVar("Heal", 5);
        WithKeywords(CardKeyword.Exhaust);
        WithTip(new TooltipSource(_ => HoverTipFactory.Static(StaticHoverTip.Block)));
        WithEnergyTip();
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Block.UpgradeValueBy(10m);
        RemoveKeyword(CardKeyword.Exhaust);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }
}
