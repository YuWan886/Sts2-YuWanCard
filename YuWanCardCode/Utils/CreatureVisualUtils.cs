using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Characters;

namespace YuWanCard.Utils;

public static class CreatureVisualUtils
{
    private const string NormalSkin = "normal";
    private const string DefaultSkin = "default";
    private const string IdleAnimation = "Idle";
    private static readonly ConditionalWeakTable<Creature, TransformationSequenceState> TransformationSequences = [];

    private sealed class TransformationSequenceState
    {
        public int SequenceId;
    }

    public static bool SwitchCreatureSkin(Creature creature, string skinName)
    {
        var megaSprite = GetMegaSprite(creature);
        if (megaSprite == null) return false;

        var skeleton = megaSprite.GetSkeleton();
        if (skeleton == null) return false;

        var data = skeleton.GetData();
        var skin = data.FindSkin(skinName);
        if (skin != null)
        {
            skeleton.SetSkin(skin);
            skeleton.SetSlotsToSetupPose();
            return true;
        }

        return false;
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

    public static void PlayPigTransformationSequence(
        Creature creature,
        string transformAnimation,
        double transformDurationSeconds,
        string transformedSkin,
        params Creature?[] linkedCreatures)
    {
        if (creature.Player?.Character is not Pig)
        {
            return;
        }

        var linkedTargets = linkedCreatures
            .Where(static creature => creature != null)
            .Cast<Creature>()
            .ToArray();

        if (!CanPlayPigTransformationSequence(creature, transformAnimation, transformedSkin))
        {
            return;
        }

        int sequenceId = NextSequenceId(creature);
        SwitchCreatureToBaseSkin(creature);
        foreach (var linkedCreature in linkedTargets)
        {
            SwitchCreatureToBaseSkin(linkedCreature);
        }

        PlayAnimation(creature, transformAnimation);

        ScheduleAfter(transformDurationSeconds, () =>
        {
            if (!IsCurrentSequence(creature, sequenceId))
            {
                return;
            }

            SwitchCreatureSkin(creature, transformedSkin);
            PlayAnimation(creature, IdleAnimation);

            foreach (var linkedCreature in linkedTargets)
            {
                if (!linkedCreature.IsAlive)
                {
                    continue;
                }

                SwitchCreatureSkin(linkedCreature, transformedSkin);
                PlayAnimation(linkedCreature, IdleAnimation);
            }
        });
    }

    public static void ResetPigTransformationVisuals(Creature creature, params Creature?[] linkedCreatures)
    {
        CancelTransformationSequence(creature);
        SwitchCreatureToBaseSkin(creature);

        foreach (var linkedCreature in linkedCreatures)
        {
            if (linkedCreature == null)
            {
                continue;
            }

            CancelTransformationSequence(linkedCreature);
            SwitchCreatureToBaseSkin(linkedCreature);
        }
    }

    private static void SwitchCreatureToBaseSkin(Creature creature)
    {
        if (SwitchCreatureSkin(creature, NormalSkin))
        {
            return;
        }

        SwitchCreatureSkin(creature, DefaultSkin);
    }

    private static bool CanPlayPigTransformationSequence(Creature creature, string transformAnimation, string transformedSkin)
    {
        var megaSprite = GetMegaSprite(creature);
        if (megaSprite == null)
        {
            return false;
        }

        string? animationName = GetAnimationNameForTrigger(transformAnimation);
        if (string.IsNullOrWhiteSpace(animationName) || !megaSprite.HasAnimation(animationName))
        {
            return false;
        }

        var skeleton = megaSprite.GetSkeleton();
        if (skeleton == null)
        {
            return false;
        }

        return skeleton.GetData().FindSkin(transformedSkin) != null;
    }

    private static string? GetAnimationNameForTrigger(string animationTrigger)
    {
        return animationTrigger.ToLowerInvariant();
    }

    private static int NextSequenceId(Creature creature)
    {
        var state = TransformationSequences.GetOrCreateValue(creature);
        return ++state.SequenceId;
    }

    private static void CancelTransformationSequence(Creature creature)
    {
        var state = TransformationSequences.GetOrCreateValue(creature);
        state.SequenceId++;
    }

    private static bool IsCurrentSequence(Creature creature, int sequenceId)
    {
        return TransformationSequences.GetOrCreateValue(creature).SequenceId == sequenceId;
    }

    private static void ScheduleAfter(double seconds, Action callback)
    {
        var tree = NCombatRoom.Instance?.GetTree();
        if (tree == null)
        {
            Callable.From(callback).CallDeferred();
            return;
        }

        var timer = tree.CreateTimer(seconds);
        timer.Timeout += callback;
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
