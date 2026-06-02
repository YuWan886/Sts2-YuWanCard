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

        int currentSlippery = Owner.GetPower<SlipperyPower>()?.Amount ?? 0;
        int toApply = Math.Min(1, 3 - currentSlippery);
        if (toApply > 0)
        {
            Flash();
            await PowerCmd.Apply<SlipperyPower>(Owner, toApply, Owner, null);
        }
    }
}
