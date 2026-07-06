using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Core.Extensions;

public static class CombatStatePlayerExtensions
{
    public static IReadOnlyList<Player> GetLivingPlayers(this ICombatState? combatState)
    {
        if (combatState == null)
        {
            return [];
        }

        List<Player> players = [];
        foreach (var player in combatState.Players)
        {
            if (player?.Creature is not { IsAlive: true, IsPet: false })
            {
                continue;
            }

            players.Add(player);
        }

        return players;
    }

    public static IReadOnlyList<Creature> GetLivingPlayerCreatures(this ICombatState? combatState)
    {
        return combatState.GetLivingPlayers()
            .Select(player => player.Creature)
            .ToList();
    }
}
