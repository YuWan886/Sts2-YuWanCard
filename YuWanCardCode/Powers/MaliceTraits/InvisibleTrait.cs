using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class InvisibleTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        if (Owner.Powers.Any(p => p is BufferPower))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<BufferPower>(Owner, Amount, Owner, null);
    }
}
