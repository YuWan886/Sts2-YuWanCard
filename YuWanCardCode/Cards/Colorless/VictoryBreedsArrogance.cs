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
public class VictoryBreedsArrogance : YuWanCardModel
{
    public VictoryBreedsArrogance() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy)
    {
        WithBlock(15, -3);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<VictoryBreedsArrogancePower>()));
    }

    public override string PortraitPath => "res://YuWanCard/images/card_portraits/sad_army_win.png";



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null || target.Monster == null)
        {
            return;
        }

        if (IntentUtils.GetAttackIntentDamage(target) <= Owner.Creature.CurrentHp)
        {
            return;
        }

        await PowerCmd.Apply<VictoryBreedsArrogancePower>(new ThrowingPlayerChoiceContext(), target, 3, Owner.Creature, this);
    }
}
