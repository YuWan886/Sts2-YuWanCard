using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Monsters;

namespace YuWanCard.Utils;

public enum FerrousWroughtnautFlank
{
    Left,
    Right
}

public static class FerrousWroughtnautPositioning
{
    private const float FlankDistance = 570f;
    private const float PlayerAnchorY = 170f;
    private const float PlayerSpacing = 115f;

    private static readonly SpireField<Creature, FerrousWroughtnautFlank> PlayerFlanks = new(() => FerrousWroughtnautFlank.Left);
    private static readonly SpireField<FerrousWroughtnaut, FerrousWroughtnautFlank> GuardianFacing = new(() => FerrousWroughtnautFlank.Left);
    private static readonly SpireField<FerrousWroughtnaut, bool> ActiveEncounters = new(() => false);

    public static void Initialize(FerrousWroughtnaut guardian, IEnumerable<Player> players)
    {
        ActiveEncounters.Set(guardian, true);
        GuardianFacing.Set(guardian, FerrousWroughtnautFlank.Left);

        foreach (Player player in players.Where(static player => player.Creature.IsAlive))
        {
            PlayerFlanks.Set(player.Creature, FerrousWroughtnautFlank.Left);
        }

        RefreshVisualPositions(guardian);
    }

    public static bool IsActive(FerrousWroughtnaut guardian)
    {
        return ActiveEncounters.Get(guardian) == true;
    }

    public static void Toggle(Creature playerCreature)
    {
        FerrousWroughtnaut? guardian = playerCreature.CombatState?.Enemies
            .Select(static enemy => enemy.Monster)
            .OfType<FerrousWroughtnaut>()
            .FirstOrDefault(IsActive);
        if (guardian == null)
        {
            return;
        }

        PlayerFlanks.Set(playerCreature, GetFlank(playerCreature) == FerrousWroughtnautFlank.Left
            ? FerrousWroughtnautFlank.Right
            : FerrousWroughtnautFlank.Left);
        RefreshVisualPositions(guardian);
    }

    public static IEnumerable<Creature> GetFrontPlayers(FerrousWroughtnaut guardian)
    {
        FerrousWroughtnautFlank front = GetFacing(guardian);
        return guardian.CombatState?.Players
            .Select(static player => player.Creature)
            .Where(creature => creature.IsAlive && GetFlank(creature) == front)
            ?? Enumerable.Empty<Creature>();
    }

    public static bool CanDamage(FerrousWroughtnaut guardian, Creature playerCreature)
    {
        return guardian.IsStaggered && GetFlank(playerCreature) != GetFacing(guardian);
    }

    public static void TurnTowardMostPlayers(FerrousWroughtnaut guardian)
    {
        if (!IsActive(guardian) || guardian.IsStaggered || guardian.CombatState == null)
        {
            return;
        }

        int leftCount = guardian.CombatState.Players.Count(player => player.Creature.IsAlive && GetFlank(player.Creature) == FerrousWroughtnautFlank.Left);
        int rightCount = guardian.CombatState.Players.Count(player => player.Creature.IsAlive && GetFlank(player.Creature) == FerrousWroughtnautFlank.Right);
        if (leftCount == rightCount)
        {
            return;
        }

        GuardianFacing.Set(guardian, leftCount > rightCount ? FerrousWroughtnautFlank.Left : FerrousWroughtnautFlank.Right);
        RefreshVisualPositions(guardian);
    }

    private static FerrousWroughtnautFlank GetFlank(Creature playerCreature)
    {
        return PlayerFlanks.Get(playerCreature);
    }

    private static FerrousWroughtnautFlank GetFacing(FerrousWroughtnaut guardian)
    {
        return GuardianFacing.Get(guardian);
    }

    private static void RefreshVisualPositions(FerrousWroughtnaut guardian)
    {
        NCreature? guardianNode = NCombatRoom.Instance?.GetCreatureNode(guardian.Creature);
        if (guardianNode == null || guardian.CombatState == null)
        {
            return;
        }

        // The enemy container is already anchored at the scene center.
        guardianNode.Position = new Vector2(0f, 170f);
        if (guardianNode.Visuals.GetCurrentBody() is Sprite2D body)
        {
            body.FlipH = GetFacing(guardian) == FerrousWroughtnautFlank.Right;
        }

        foreach (FerrousWroughtnautFlank flank in Enum.GetValues<FerrousWroughtnautFlank>())
        {
            var players = guardian.CombatState.Players
                .Where(player => player.Creature.IsAlive && GetFlank(player.Creature) == flank)
                .OrderBy(static player => player.NetId)
                .ToList();
            for (int index = 0; index < players.Count; index++)
            {
                NCreature? playerNode = NCombatRoom.Instance?.GetCreatureNode(players[index].Creature);
                if (playerNode == null)
                {
                    continue;
                }

                float centeredIndex = index - (players.Count - 1) * 0.5f;
                float x = flank == FerrousWroughtnautFlank.Left ? -FlankDistance : FlankDistance;
                float y = PlayerAnchorY + centeredIndex * PlayerSpacing;
                playerNode.Position = new Vector2(x, y);
                FaceGuardian(playerNode, flank);
            }
        }
    }

    private static void FaceGuardian(NCreature playerNode, FerrousWroughtnautFlank flank)
    {
        Node2D body = playerNode.Visuals.GetCurrentBody();
        float horizontalScale = MathF.Abs(body.Scale.X);
        body.Scale = new Vector2(
            flank == FerrousWroughtnautFlank.Left ? horizontalScale : -horizontalScale,
            body.Scale.Y);
    }
}
