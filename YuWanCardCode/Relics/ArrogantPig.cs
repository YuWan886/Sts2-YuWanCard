using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class ArrogantPig : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public ArrogantPig() : base(true)
    {
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {

        if (Owner == null)
        {
            return 0m;
        }

        if (target == null)
        {
            return 0m;
        }

        if (target.Player == null)
        {
            return 0m;
        }

        if (dealer == null)
        {
            return 0m;
        }

        // 只有当玩家受到伤害时才触发
        if (target.Player == Owner)
        {
            return 2m;
        }
        else
        {
            return 0m;
        }
    }

    public decimal ModifyVulnerableMultiplier(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 如果目标是敌人且敌人有易伤，增加 50% 伤害
        if (target != Owner?.Creature && dealer == Owner?.Creature)
        {
            return amount + 0.5m;
        }
        return amount;
    }

    public decimal ModifyWeakMultiplier(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 如果来源是敌人且敌人有虚弱，减少 30% 伤害
        if (target == Owner?.Creature && dealer != Owner?.Creature)
        {
            return amount - 0.30m;
        }
        return amount;
    }
}
