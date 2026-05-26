using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ShulkerTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<SlipperyPower>(Owner, 1 * Amount, Owner, null);
    }
}
