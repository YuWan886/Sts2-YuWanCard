using MegaCrit.Sts2.Core.Entities.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers.MaliceTraits;

public abstract class MaliceTraitPowerBase : YuWanPowerModel
{
    public sealed override PowerType Type => PowerType.Buff;
    public sealed override PowerStackType StackType => PowerStackType.Counter;
}
