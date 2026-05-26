using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class GrowthTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("GrowthStrength", 2m)];
    protected override string[] AutoUpdateVarNames => ["GrowthStrength"];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner, 2 * Amount, Owner, null);
    }
}
