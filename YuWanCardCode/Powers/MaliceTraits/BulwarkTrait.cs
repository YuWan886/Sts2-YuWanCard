using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class BulwarkTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BulwarkBlock", 6m)];
    protected override string[] AutoUpdateVarNames => ["BulwarkBlock"];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int actIndex = combatState.RunState?.CurrentActIndex ?? 0;
        int blockAmount = (6 + 2 * Math.Min(Math.Max(actIndex, 0), 2)) * (int)Amount;

        Flash();
        await CreatureCmd.GainBlock(Owner, blockAmount, ValueProp.Unpowered, null);
    }
}
