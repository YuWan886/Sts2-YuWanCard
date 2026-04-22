using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class CreatureHeightUtils
{
    public static float GetCreatureHeight(Creature creature)
    {
        if (creature == null) return 0f;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return 0f;

        var visuals = creatureNode.Visuals;
        if (visuals == null) return 0f;

        var bounds = visuals.GetNodeSafe<Control>("Bounds", logWarning: false);
        if (bounds == null) return 0f;

        float height = bounds.OffsetBottom - bounds.OffsetTop;
        return Math.Abs(height);
    }

    public static bool IsTallCreature(Creature creature, float heightThreshold)
    {
        return GetCreatureHeight(creature) > heightThreshold;
    }
}
