using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class CreatureVisualUtils
{
    public static void SwitchCreatureSkin(Creature creature, string skinName)
    {
        var megaSprite = GetMegaSprite(creature);
        if (megaSprite == null) return;

        var skeleton = megaSprite.GetSkeleton();
        if (skeleton == null) return;

        var data = skeleton.GetData();
        var skin = data.FindSkin(skinName);
        if (skin != null)
        {
            skeleton.SetSkin(skin);
            skeleton.SetSlotsToSetupPose();
        }
    }

    public static void PlayAnimation(Creature creature, string animationName)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        creatureNode?.SetAnimationTrigger(animationName);
    }
    public static void PlayAnimationLoop(Creature creature, string animationName, bool loop)
    {
        var megaSprite = GetMegaSprite(creature);
        megaSprite?.GetAnimationState()?.SetAnimation(animationName, loop);
    }

    private static MegaSprite? GetMegaSprite(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode?.Visuals == null) return null;

        var spineNode = creatureNode.Visuals.GetNode("%Visuals");
        if (spineNode == null) return null;

        return new MegaSprite(spineNode);
    }
}
