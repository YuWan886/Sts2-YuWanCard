using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class GravityTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("GravityAmount", 1m)];
    protected override string[] AutoUpdateVarNames => ["GravityAmount"];

    private class Data
    {
        public int TickCount;
        public int ReducedAmount;
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
        if (data.TickCount < 2)
        {
            return;
        }

        data.TickCount = 0;
        int maxReduction = 2 + combatState.RunState!.CurrentActIndex;
        int remainingReduction = maxReduction - data.ReducedAmount;
        if (remainingReduction <= 0)
        {
            return;
        }

        int reduction = Math.Min((int)Amount, remainingReduction);
        if (reduction <= 0)
        {
            return;
        }

        data.ReducedAmount += reduction;
        Flash();

        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead)
            {
                continue;
            }

            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), player.Creature, -reduction, Owner, null);
        }
    }
}
