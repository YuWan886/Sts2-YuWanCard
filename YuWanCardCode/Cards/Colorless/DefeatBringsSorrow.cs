using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Powers;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.HoverTips;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class DefeatBringsSorrow : YuWanCardModel
{
    protected override bool IsPlayable => IntentUtils.AnyEnemyIntendsToAttack(CombatState);

    public DefeatBringsSorrow() : base(
        baseCost: 2,
        type: CardType.Power,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<DefeatBringsSorrowPower>()));
        WithCostUpgradeBy(-1);
    }

    public override string PortraitPath => "res://YuWanCard/images/card_portraits/sad_army_win.png";



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DefeatBringsSorrowPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
    }
}
