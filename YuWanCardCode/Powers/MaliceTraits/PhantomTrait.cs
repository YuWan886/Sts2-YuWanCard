using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class PhantomTrait : MaliceTraitPowerBase
{
    private const int Interval = 4;

    private class Data
    {
        public int TickCount;
    }

    protected override object InitInternalData() => new Data();

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.TickCount++;
        if (data.TickCount % Interval != 0)
        {
            return;
        }

        if (Owner.HasPower<IntangiblePower>())
        {
            return;
        }

        // Intangible ticks down at the enemy's own turn end, so apply 2 to ensure
        // one stack survives into the following player turn where damage matters.
        Flash();
        await PowerCmd.Apply<IntangiblePower>(Owner, 1 + Amount, Owner, null);
    }
}
