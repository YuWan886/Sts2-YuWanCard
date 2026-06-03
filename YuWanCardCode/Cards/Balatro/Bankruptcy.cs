using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Bankruptcy : YuWanCardModel
{
    public Bankruptcy() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null || Owner.Gold <= 0)
        {
            return;
        }

        int gold = Owner.Gold;
        await PlayerCmd.LoseGold(gold, Owner, GoldLossType.Spent);

        decimal ratio = IsUpgraded ? 0.75m : 0.5m;
        decimal damage = Math.Max(1m, Math.Floor(gold * ratio));
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, damage, ValueProp.Move, Owner.Creature, this);
    }
}
