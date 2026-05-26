using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SpeedyTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead)
            {
                continue;
            }

            Flash();
            await PowerCmd.Apply<DexterityPower>(player.Creature, -Amount, Owner, null);
        }
    }
}
