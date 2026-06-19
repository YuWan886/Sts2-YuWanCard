using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class EnervateTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("EnergyLoss", 1m)];
    protected override string[] AutoUpdateVarNames => ["EnergyLoss"];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        bool flashed = false;
        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead || player.Creature.GetPower<CorrosionEnergyLossPower>() != null)
            {
                continue;
            }

            if (!flashed)
            {
                Flash();
                flashed = true;
            }

            await PowerCmd.Apply<CorrosionEnergyLossPower>(player.Creature, Amount, Owner, null);
        }
    }
}
