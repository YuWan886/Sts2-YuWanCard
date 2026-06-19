using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class PhantomTrait : MaliceTraitPowerBase
{
    private const int Interval = 3;

    private class Data
    {
        public int TickCount;
    }

    protected override object InitInternalData() => new Data();

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
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
        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), Owner, 1 + Amount, Owner, null);
    }
}
