using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class RegenTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RegenPercent", 5m)];
    protected override string[] AutoUpdateVarNames => ["RegenPercent"];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int healAmount = Math.Max(1, (int)Math.Ceiling(Owner.MaxHp * 0.05m * Amount));
        Flash();
        await CreatureCmd.Heal(Owner, healAmount, playAnim: true);
    }
}
