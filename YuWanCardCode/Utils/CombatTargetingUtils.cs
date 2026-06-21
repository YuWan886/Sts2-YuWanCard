using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Utils;

internal static class CombatTargetingUtils
{
    public static Creature? GetDeterministicRandomLivingEnemy(Player? owner)
    {
        if (owner?.Creature?.CombatState == null)
        {
            return null;
        }

        return GetDeterministicRandomTarget(owner, owner.Creature.CombatState.Enemies.Where(enemy => !enemy.IsDead));
    }

    public static Creature? GetDeterministicRandomTarget(Player? owner, IEnumerable<Creature>? candidates)
    {
        if (owner == null || candidates == null)
        {
            return null;
        }

        List<Creature> candidateList = candidates.ToList();
        return candidateList.Count == 0
            ? null
            : owner.RunState.Rng.CombatTargets.NextItem(candidateList);
    }
}
