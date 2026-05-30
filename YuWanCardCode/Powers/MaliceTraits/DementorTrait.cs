using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class DementorTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead || player.Creature.HasPower<ChainsOfBindingPower>())
            {
                continue;
            }

            Flash();
            await PowerCmd.Apply<ChainsOfBindingPower>(new ThrowingPlayerChoiceContext(), player.Creature, 2, Owner, null);
        }
    }
}
