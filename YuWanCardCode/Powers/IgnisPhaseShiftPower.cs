using MegaCrit.Sts2.Core.Entities.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public sealed class IgnisPhaseShiftPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;
}
