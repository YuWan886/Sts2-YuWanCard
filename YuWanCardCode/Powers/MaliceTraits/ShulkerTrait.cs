using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ShulkerTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int currentSlippery = Owner.GetPower<SlipperyPower>()?.Amount ?? 0;
        int toApply = Math.Min(2, 3 - currentSlippery);
        if (toApply > 0)
        {
            Flash();
            await PowerCmd.Apply<SlipperyPower>(new ThrowingPlayerChoiceContext(), Owner, toApply, Owner, null);
        }
    }
}
