using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigSlam : YuWanCardModel
{
    public PigSlam() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithVar("Repeat", 2);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = DynamicVars["Repeat"].IntValue;

        // 如果目标敌人有增益效果，额外命中一次
        if (cardPlay.Target != null && cardPlay.Target.Powers.Any(p => p.Type == PowerType.Buff))
        {
            hitCount++;
        }

        await CommonActions.CardAttack(this, cardPlay, hitCount: hitCount).Execute(choiceContext);
    }
}
