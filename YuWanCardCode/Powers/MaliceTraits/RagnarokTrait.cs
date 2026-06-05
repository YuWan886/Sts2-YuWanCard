using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class RagnarokTrait : MaliceTraitPowerBase
{
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return Task.CompletedTask;
        }

        // Collect all players that have at least one non-disabled relic
        var candidates = combatState.Players
            .Where(p => !p.Creature.IsDead && p.Relics.Any(r => r.Status != RelicStatus.Disabled))
            .ToList();

        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rng = combatState.RunState!.Rng!.CombatTargets;
        var targetPlayer = rng.NextItem(candidates)!;

        var availableRelics = targetPlayer.Relics
            .Where(r => r.Status != RelicStatus.Disabled)
            .ToList();

        if (availableRelics.Count == 0)
        {
            return Task.CompletedTask;
        }

        var targetRelic = rng.NextItem(availableRelics)!;
        Flash();
        targetRelic.Status = RelicStatus.Disabled;
        return Task.CompletedTask;
    }
}
