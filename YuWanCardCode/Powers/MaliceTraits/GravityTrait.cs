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
        Flash();

        int maxReduction = 2 + combatState.RunState!.CurrentActIndex;
        int reduction = Math.Min((int)Amount, maxReduction);

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
