using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class UndyingTrait : MaliceTraitPowerBase
{
    private class Data
    {
        public bool Triggered;
    }

    protected override object InitInternalData() => new Data();

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner)
        {
            return true;
        }

        return GetInternalData<Data>().Triggered;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.Triggered)
        {
            return;
        }

        data.Triggered = true;
        Flash();
        await CreatureCmd.Heal(Owner, Math.Max(1, Owner.MaxHp / 2), playAnim: true);
        await PowerCmd.Remove(this);
    }
}
