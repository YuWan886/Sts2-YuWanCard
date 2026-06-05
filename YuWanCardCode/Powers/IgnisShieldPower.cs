using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public sealed class IgnisShieldPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
    

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0)
        {
            return amount;
        }

        if (dealer == null || dealer.Side == Owner.Side)
        {
            return amount;
        }

        if (amount <= Amount)
        {
            return amount;
        }

        Flash();
        return Amount;
    }
}
