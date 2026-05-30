using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SpeedyTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
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
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), player.Creature, -Amount, Owner, null);
        }
    }
}
