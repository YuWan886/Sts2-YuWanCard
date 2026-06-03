using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Modifiers;

namespace YuWanCard.Relics.Balatro;

public abstract class BalatroRelicModel : YuWanRelicModel
{
    protected BalatroRelicModel() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState)
    {
        return BalatroModifier.IsActive(runState);
    }
}
