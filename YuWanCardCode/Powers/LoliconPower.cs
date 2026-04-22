using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class LoliconPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private const float HeightThreshold = 140f;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (target == null) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        if (CreatureHeightUtils.IsTallCreature(target, HeightThreshold))
        {
            return 2m;
        }

        return 1m;
    }
}
