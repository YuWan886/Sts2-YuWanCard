using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace YuWanCard.Utils;

public static class IntentUtils
{
    public static bool AnyEnemyIntendsToAttack(CombatState? combatState)
    {
        if (combatState == null)
        {
            return false;
        }

        return combatState.Enemies.Any(enemy =>
            enemy.IsAlive &&
            enemy.Monster?.IntendsToAttack == true);
    }

    public static int GetEnemyAttackIntentDamageTotal(CombatState? combatState)
    {
        if (combatState == null)
        {
            return 0;
        }

        return combatState.Enemies
            .Where(enemy => enemy.IsAlive && enemy.Monster != null)
            .Sum(GetAttackIntentDamage);
    }

    public static int GetAttackIntentDamage(Creature creature)
    {
        if (creature.Monster?.NextMove == null || creature.CombatState == null)
        {
            return 0;
        }

        return creature.Monster.NextMove.Intents
            .OfType<AttackIntent>()
            .Sum(intent => intent.GetTotalDamage(creature.CombatState.PlayerCreatures, creature));
    }

    public static string? GetCurrentMoveFollowUpStateId(Creature creature)
    {
        var move = creature.Monster?.NextMove;
        return move?.FollowUpState?.Id ?? move?.FollowUpStateId;
    }
}
