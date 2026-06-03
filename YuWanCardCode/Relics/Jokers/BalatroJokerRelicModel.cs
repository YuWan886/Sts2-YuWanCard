using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Modifiers;

namespace YuWanCard.Relics.Balatro;

public abstract class YuWanJokerRelicModel : YuWanRelicModel
{
    protected YuWanJokerRelicModel()
    {
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override bool IsAllowedInShops => false;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        Player? owner = Owner;
        if (owner == null)
        {
            return;
        }

        BalatroModifier? modifier = owner.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
        if (modifier != null)
        {
            await modifier.AcquireJoker(this, owner);
        }

        await RelicCmd.Remove(this);
    }
}
