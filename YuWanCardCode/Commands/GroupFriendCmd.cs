using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using YuWanCard.Cards;

namespace YuWanCard.Commands;

public static class GroupFriendCmd
{
    public const string PigVisualScenePath = "res://YuWanCard/scenes/characters/pig.tscn";

    private const string PigVisualRootName = "GroupFriendPigVisual";

    private static readonly string[] HiddenSpritePaths =
    [
        "SpineSword/SwordBone/ScaleContainer/Blade",
        "SpineSword/SwordBone/ScaleContainer/SteppedFireMix",
        "SpineSword/SwordBone/ScaleContainer/Blade2",
        "SpineSword/SwordBone/ScaleContainer/BladeGlow"
    ];

    private static readonly string[] HiddenTextureRectPaths =
    [
        "SpineSword/SwordBone/ScaleContainer/BladeOutline2",
        "SpineSword/SwordBone/ScaleContainer/Detail",
        "SpineSword/SwordBone/ScaleContainer/Hilt",
        "SpineSword/SwordBone/ScaleContainer/Hilt2"
    ];

    private static readonly string[] HiddenCanvasItemPaths =
    [
        "SpineSword/SwordBone/ScaleContainer/YellowDots",
        "SpineSword/SwordBone/ScaleContainer/light_small2"
    ];

    private static readonly string[] HiddenParticlePaths =
    [
        "SpineSword/SwordBone/ScaleContainer/middle spike",
        "SpineSword/SwordBone/ScaleContainer/SpikeCircle",
        "SpineSword/SwordBone/ScaleContainer/SpikeCircle2",
        "SpineSword/SwordBone/ScaleContainer/Spikes",
        "SpineSword/SwordBone/ScaleContainer/Spikes2"
    ];

    public static async Task RefreshGroupFriend(decimal amount, Player player)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        List<GroupFriendImpact> impacts = GetGroupFriendImpacts(player, includeExhausted: false).ToList();
        if (amount > 0 && impacts.Count == 0)
        {
            if (player.Creature.CombatState == null)
            {
                return;
            }

            GroupFriendImpact impact = player.Creature.CombatState.CreateCard<GroupFriendImpact>(player);
            impact.CreatedThroughGroupFriend = true;
            CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(impact, PileType.Hand, player);
            CardCmd.PreviewCardPileAdd(addResult, 2f);
            impacts.Add(impact);
        }

        RefreshCombatRoomGroupFriendVfx(player, impacts);
    }

    public static void PlayCombatRoomGroupFriendVfx(Player player, GroupFriendImpact card)
    {
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null)
        {
            return;
        }

        NSovereignBladeVfx? vfxNode = GroupFriendImpact.GetVfxNode(player, card);
        bool isNewNode = vfxNode == null;
        if (isNewNode)
        {
            vfxNode = NSovereignBladeVfx.Create(card);
            if (vfxNode == null)
            {
                return;
            }

            DecorateVfx(vfxNode);
            creatureNode.AddChildSafely(vfxNode);
            vfxNode.Position = Vector2.Zero;
            SfxCmd.Play("event:/sfx/characters/regent/regent_forge");
        }
        else if (vfxNode != null)
        {
            DecorateVfx(vfxNode);
            SfxCmd.Play("event:/sfx/characters/regent/regent_refine");
        }

        if (vfxNode == null)
        {
            return;
        }

        vfxNode.Forge(card.CurrentDisplayDamage, isNewNode);
    }

    private static IEnumerable<GroupFriendImpact> GetGroupFriendImpacts(Player player, bool includeExhausted)
    {
        return (player.PlayerCombatState?.AllCards ?? [])
            .Where(card => !card.IsDupe)
            .Where(card => includeExhausted || card.Pile?.Type != PileType.Exhaust)
            .OfType<GroupFriendImpact>();
    }

    private static void RefreshCombatRoomGroupFriendVfx(Player player, IReadOnlyList<GroupFriendImpact> impacts)
    {
        if (impacts.Count == 0)
        {
            return;
        }

        foreach (GroupFriendImpact impact in impacts)
        {
            PlayCombatRoomGroupFriendVfx(player, impact);
        }

        for (int i = 0; i < impacts.Count; i++)
        {
            NSovereignBladeVfx? vfxNode = GroupFriendImpact.GetVfxNode(player, impacts[i]);
            if (vfxNode != null)
            {
                vfxNode.OrbitProgress = (float)i / impacts.Count;
            }
        }
    }

    private static void DecorateVfx(NSovereignBladeVfx vfxNode)
    {
        foreach (string path in HiddenSpritePaths)
        {
            if (vfxNode.GetNodeOrNull<Sprite2D>(path) is { } sprite)
            {
                sprite.Texture = null;
            }
        }

        foreach (string path in HiddenTextureRectPaths)
        {
            if (vfxNode.GetNodeOrNull<TextureRect>(path) is { } textureRect)
            {
                textureRect.Texture = null;
            }
        }

        foreach (string path in HiddenCanvasItemPaths)
        {
            if (vfxNode.GetNodeOrNull<CanvasItem>(path) is { } canvasItem)
            {
                canvasItem.Visible = false;
            }
        }

        foreach (string path in HiddenParticlePaths)
        {
            if (vfxNode.GetNodeOrNull<GpuParticles2D>(path) is { } particles)
            {
                particles.Emitting = false;
                particles.Visible = false;
                particles.Amount = 0;
                particles.Texture = null;
            }
        }

        Node2D? scaleContainer = vfxNode.GetNodeOrNull<Node2D>("SpineSword/SwordBone/ScaleContainer");
        if (scaleContainer == null || scaleContainer.GetNodeOrNull<Node2D>(PigVisualRootName) != null)
        {
            return;
        }

        PackedScene pigScene = PreloadManager.Cache.GetScene(PigVisualScenePath);
        Node2D? pigVisualRoot = pigScene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        if (pigVisualRoot == null)
        {
            MainFile.Logger.Warn("GroupFriendCmd: failed to instantiate pig visuals for group friend VFX");
            return;
        }

        pigVisualRoot.Name = PigVisualRootName;
        pigVisualRoot.Scale = Vector2.One * 2.25f;
        pigVisualRoot.Rotation = -Mathf.Pi / 2f;

        Marker2D? centerMarker = pigVisualRoot.GetNodeOrNull<Marker2D>("%CenterPos")
            ?? pigVisualRoot.GetNodeOrNull<Marker2D>("CenterPos");
        if (centerMarker != null)
        {
            pigVisualRoot.Position = -centerMarker.Position.Rotated(pigVisualRoot.Rotation);
        }

        scaleContainer.AddChildSafely(pigVisualRoot);

        Node2D? visualsNode = pigVisualRoot.GetNodeOrNull<Node2D>("%Visuals")
            ?? pigVisualRoot.GetNodeOrNull<Node2D>("Visuals");
        if (visualsNode != null)
        {
            vfxNode.RunWhenSpineReady(new MegaSprite(visualsNode), state => state.SetAnimation("idle_loop"));
        }
    }
}
