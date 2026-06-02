using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class EnemySpawnPositionUtils
{
    private const float MinimumHorizontalSpacing = 120f;
    private const float ExtraHorizontalPadding = 20f;
    private const float VerticalStaggerFactor = 0.08f;
    private const int MaxPlacementAttempts = 8;

    public static string? GetNextEnemySlot(CombatState combatState)
    {
        string? slotName = combatState.Encounter?.GetNextSlot(combatState);
        return string.IsNullOrEmpty(slotName) ? null : slotName;
    }

    public static Vector2 GetCreatureCenterPosition(Creature creature)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node == null)
        {
            return Vector2.Zero;
        }

        return node.Position;
    }

    public static void PositionSummonWithoutSlot(Creature summon, Creature? anchorCreature = null)
    {
        var room = NCombatRoom.Instance;
        var summonNode = room?.GetCreatureNode(summon);
        if (summonNode == null)
        {
            return;
        }

        Vector2 anchorCenter = GetCreatureCenterPosition(anchorCreature ?? summon);
        if (anchorCreature != null && room?.GetCreatureNode(anchorCreature) is { } anchorNode)
        {
            anchorCenter = anchorNode.Position;
        }

        float summonWidth = summonNode.Hitbox.Size.X;
        float summonHeight = summonNode.Hitbox.Size.Y;
        float spacing = Math.Max(MinimumHorizontalSpacing, summonWidth + ExtraHorizontalPadding);

        var occupiedNodes = summon.CombatState?.Enemies
            .Where(enemy => enemy != summon && enemy.IsAlive)
            .Select(enemy => room?.GetCreatureNode(enemy))
            .Where(node => node != null)
            .Select(node => node!)
            .ToList();

        if (occupiedNodes == null || occupiedNodes.Count == 0)
        {
            SetNodeCenterPosition(summonNode, anchorCenter);
            return;
        }

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            float offsetIndex = attempt == 0 ? 0f : (attempt % 2 == 1 ? (attempt + 1) / 2f : -(attempt / 2f));
            Vector2 candidateCenter = anchorCenter + new Vector2(
                spacing * offsetIndex,
                Math.Abs(offsetIndex) * summonHeight * VerticalStaggerFactor);

            bool overlaps = occupiedNodes.Any(node =>
            {
                float requiredSpacing = Math.Max(spacing, (summonNode.Hitbox.Size.X + node.Hitbox.Size.X) * 0.5f + ExtraHorizontalPadding);
                Vector2 nodeCenter = node.Position;
                return Math.Abs(candidateCenter.X - nodeCenter.X) < requiredSpacing;
            });

            if (!overlaps)
            {
                SetNodeCenterPosition(summonNode, candidateCenter);
                return;
            }
        }

        SetNodeCenterPosition(summonNode, anchorCenter + new Vector2(spacing, summonHeight * VerticalStaggerFactor));
    }

    public static void SpreadSummonsAroundPosition(IReadOnlyList<Creature> summons, Vector2 centerPosition)
    {
        var room = NCombatRoom.Instance;
        if (room == null || summons.Count == 0)
        {
            return;
        }

        var summonNodes = summons
            .Select(room.GetCreatureNode)
            .Where(node => node != null)
            .Select(node => node!)
            .ToList();

        if (summonNodes.Count == 0)
        {
            return;
        }

        float maxWidth = summonNodes.Max(node => node.Hitbox.Size.X);
        float spacing = Math.Max(MinimumHorizontalSpacing, maxWidth + ExtraHorizontalPadding);
        float startOffset = -spacing * (summonNodes.Count - 1) * 0.5f;
        float centerIndex = (summonNodes.Count - 1) * 0.5f;

        for (int i = 0; i < summonNodes.Count; i++)
        {
            var node = summonNodes[i];
            float xOffset = startOffset + i * spacing;
            float yOffset = Math.Abs(i - centerIndex) * node.Hitbox.Size.Y * VerticalStaggerFactor;
            SetNodeCenterPosition(node, centerPosition + new Vector2(xOffset, yOffset));
        }
    }

    private static void SetNodeCenterPosition(NCreature node, Vector2 centerPosition)
    {
        node.Position = centerPosition;
    }
}
