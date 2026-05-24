using MegaCrit.Sts2.Core.Entities.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public class HextechPigletGuardMinionPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    protected override string IconBasePath => "res://YuWanCard/images/integrations/hextech/powers/hextech_piglet_guard_minion_power.png";
}
