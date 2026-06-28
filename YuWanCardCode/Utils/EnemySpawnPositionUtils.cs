using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class EnemySpawnPositionUtils
{
    private const float MinimumHorizontalSpacing = 120f;
    private const float ExtraHorizontalPadding = 20f;
    private const float MinimumVerticalGap = 32f;
    private const float ExtraVerticalPadding = 12f;
    private const float VerticalStaggerFactor = 0.08f;
    private const int MaxPlacementRows = 5;
    private const int MaxHorizontalOffsetsPerRow = 4;

    public static string? GetNextEnemySlot(ICombatState combatState)
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

        return GetHitboxCenter(node);
    }

    public static bool TryGetCreatureHitboxRect(Creature creature, out Rect2 rect)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node == null || !TryGetHitboxRect(node, out rect))
        {
            rect = default;
            return false;
        }

        return true;
    }

    public static async Task PositionSummonWithoutSlot(Creature summon, Creature? anchorCreature = null, Rect2? anchorRectOverride = null)
    {
        var room = NCombatRoom.Instance;
        var summonNode = room?.GetCreatureNode(summon);
        if (summonNode == null)
        {
            return;
        }

        if (!await EnsureHitboxReadyAsync(summonNode))
        {
            return;
        }

        Rect2? anchorRect = anchorRectOverride;
        if (anchorRect == null && anchorCreature != null)
        {
            var anchorNode = room?.GetCreatureNode(anchorCreature);
            if (anchorNode != null && await EnsureHitboxReadyAsync(anchorNode) && TryGetHitboxRect(anchorNode, out Rect2 liveAnchorRect))
            {
                anchorRect = liveAnchorRect;
            }
        }

        if (anchorRect == null)
        {
            return;
        }

        var occupiedNodes = summon.CombatState?.Enemies
            .Where(enemy => enemy != summon && enemy.IsAlive)
            .Select(enemy => room?.GetCreatureNode(enemy))
            .Where(node => node != null)
            .Select(node => node!)
            .ToList();

        if (occupiedNodes == null)
        {
            occupiedNodes = [];
        }

        Vector2 summonSize = summonNode.Hitbox.Size;
        Vector2 anchorTopCenter = GetTopCenter(anchorRect.Value);
        float horizontalSpacing = Math.Max(
            MinimumHorizontalSpacing,
            Math.Max(summonSize.X, occupiedNodes.Count > 0 ? occupiedNodes.Max(node => node.Hitbox.Size.X) : summonSize.X) + ExtraHorizontalPadding);
        float verticalStep = summonSize.Y + MinimumVerticalGap;
        Vector2 preferredCenter = new(
            anchorTopCenter.X,
            anchorTopCenter.Y - MinimumVerticalGap - summonSize.Y * 0.5f);

        List<Rect2> occupiedRects = occupiedNodes
            .Select(TryGetStableHitboxRect)
            .Where(rect => rect.HasValue)
            .Select(rect => rect!.Value)
            .ToList();

        for (int row = 0; row <= MaxPlacementRows; row++)
        {
            float centerY = preferredCenter.Y - row * verticalStep;
            foreach (float offsetIndex in EnumerateHorizontalOffsets())
            {
                Vector2 candidateCenter = new(anchorTopCenter.X + offsetIndex * horizontalSpacing, centerY);
                Rect2 candidateRect = CreateRectFromCenter(candidateCenter, summonSize);

                if (OverlapsAny(candidateRect, occupiedRects))
                {
                    continue;
                }

                SetNodeHitboxCenterPosition(summonNode, candidateCenter);
                return;
            }
        }

        SetNodeHitboxCenterPosition(summonNode, preferredCenter - new Vector2(0f, verticalStep));
    }

    public static async Task PositionSummonsWithoutSlotsAboveAnchor(IReadOnlyList<Creature> summons, Creature anchorCreature, Rect2? anchorRectOverride = null)
    {
        foreach (Creature summon in summons)
        {
            await PositionSummonWithoutSlot(summon, anchorCreature, anchorRectOverride);
        }
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
            SetNodeHitboxCenterPosition(node, centerPosition + new Vector2(xOffset, yOffset));
        }
    }

    private static IEnumerable<float> EnumerateHorizontalOffsets()
    {
        yield return 0f;

        for (int offset = 1; offset <= MaxHorizontalOffsetsPerRow; offset++)
        {
            yield return offset;
            yield return -offset;
        }
    }

    private static Rect2 GetHitboxRect(NCreature node)
        => new(node.Hitbox.GlobalPosition, node.Hitbox.Size);

    private static Vector2 GetTopCenter(Rect2 rect)
        => rect.Position + new Vector2(rect.Size.X * 0.5f, 0f);

    private static Vector2 GetHitboxCenter(NCreature node)
        => node.Hitbox.GlobalPosition + node.Hitbox.Size * 0.5f;

    private static Rect2? TryGetStableHitboxRect(NCreature node)
        => TryGetHitboxRect(node, out Rect2 rect) ? rect : null;

    private static bool TryGetHitboxRect(NCreature node, out Rect2 rect)
    {
        if (node.Hitbox == null || node.Hitbox.Size == Vector2.Zero)
        {
            rect = default;
            return false;
        }

        rect = GetHitboxRect(node);
        return true;
    }

    private static Rect2 CreateRectFromCenter(Vector2 centerPosition, Vector2 size)
        => new(centerPosition - size * 0.5f, size);

    private static bool OverlapsAny(Rect2 candidateRect, IReadOnlyList<Rect2> occupiedRects)
    {
        Rect2 expandedCandidate = ExpandRect(candidateRect, ExtraHorizontalPadding * 0.5f, ExtraVerticalPadding);
        return occupiedRects.Any(rect => expandedCandidate.Intersects(ExpandRect(rect, ExtraHorizontalPadding * 0.5f, ExtraVerticalPadding)));
    }

    private static Rect2 ExpandRect(Rect2 rect, float horizontalPadding, float verticalPadding)
        => new(
            rect.Position - new Vector2(horizontalPadding, verticalPadding),
            rect.Size + new Vector2(horizontalPadding * 2f, verticalPadding * 2f));

    private static void SetNodeHitboxCenterPosition(NCreature node, Vector2 centerPosition)
    {
        Vector2 hitboxOffset = node.Hitbox.GlobalPosition - node.GlobalPosition;
        node.GlobalPosition = centerPosition - hitboxOffset - node.Hitbox.Size * 0.5f;
    }

    private static async Task<bool> EnsureHitboxReadyAsync(NCreature node)
    {
        for (int i = 0; i < 3; i++)
        {
            if (TryGetHitboxRect(node, out _))
            {
                return true;
            }

            if (!node.IsValid() || !node.IsInsideTree())
            {
                return false;
            }

            await node.AwaitProcessFrame();
        }

        return TryGetHitboxRect(node, out _);
    }
}
