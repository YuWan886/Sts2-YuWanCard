using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class RagnarokTrait : MaliceTraitPowerBase
{
    private class Data
    {
        public int TickCount;
    }

    protected override object InitInternalData() => new Data();

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.TickCount++;
        if (data.TickCount < 4)
        {
            return;
        }

        data.TickCount = 0;

        Flash();
        int damage = 15 * Amount;
        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead)
            {
                continue;
            }

            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), player.Creature, damage, ValueProp.Unblockable | ValueProp.Unpowered, Owner, null);
        }
    }
}
