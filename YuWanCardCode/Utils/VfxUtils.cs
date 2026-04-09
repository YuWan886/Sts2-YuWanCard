using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class VfxUtils
{
    private static readonly ConcurrentDictionary<string, PackedScene> SceneCache = new();

    private static PackedScene? GetOrLoadScene(string scenePath)
    {
        if (SceneCache.TryGetValue(scenePath, out var cachedScene))
        {
            return cachedScene;
        }

        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"VfxUtils: Failed to load scene: {scenePath}");
            return null;
        }

        SceneCache[scenePath] = scene;
        return scene;
    }

    public static Control? PlayCentered(string scenePath)
    {
        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer == null)
        {
            MainFile.Logger.Warn("VfxUtils: CombatVfxContainer not found, cannot play centered effect");
            return null;
        }

        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
        {
            return null;
        }

        var effect = scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        if (effect == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate effect from: {scenePath}");
            return null;
        }

        vfxContainer.AddChildSafely(effect);
        var game = NGame.Instance;
        if (game != null)
        {
            var viewportRect = game.GetViewportRect();
            effect.Position = viewportRect.Size * 0.5f - effect.Size * 0.5f;
        }

        MainFile.Logger.Debug($"VfxUtils: Played centered effect: {scenePath}");
        return effect;
    }

    public static Control? PlayAt(string scenePath, Vector2 position)
    {
        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer == null)
        {
            MainFile.Logger.Warn("VfxUtils: CombatVfxContainer not found, cannot play effect at position");
            return null;
        }

        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
        {
            return null;
        }

        var effect = scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        if (effect == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate effect from: {scenePath}");
            return null;
        }

        vfxContainer.AddChildSafely(effect);
        effect.Position = position - effect.Size * 0.5f;

        MainFile.Logger.Debug($"VfxUtils: Played effect at position {position}: {scenePath}");
        return effect;
    }

    public static Control? PlayAtCreature(string scenePath, Creature creature)
    {
        if (creature == null)
        {
            MainFile.Logger.Warn("VfxUtils: Creature is null, cannot play effect at creature position");
            return null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            MainFile.Logger.Warn($"VfxUtils: Could not get creature node for creature");
            return null;
        }

        var position = creatureNode.Position;
        return PlayAt(scenePath, position);
    }

    public static (Control? effect, AudioStreamPlayer? audioPlayer) PlayWithSound(string scenePath, string soundPath)
    {
        var effect = PlayCentered(scenePath);

        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer == null)
        {
            MainFile.Logger.Warn("VfxUtils: CombatVfxContainer not found, cannot play sound");
            return (effect, null);
        }

        var audioStream = GD.Load<AudioStream>(soundPath);
        if (audioStream == null)
        {
            MainFile.Logger.Warn($"VfxUtils: Failed to load audio: {soundPath}");
            return (effect, null);
        }

        var audioPlayer = new AudioStreamPlayer
        {
            Stream = audioStream,
            Bus = "SFX"
        };

        vfxContainer.AddChildSafely(audioPlayer);
        audioPlayer.Play();

        MainFile.Logger.Debug($"VfxUtils: Played effect with sound: {scenePath}, {soundPath}");
        return (effect, audioPlayer);
    }

    public static Node2D? PlayAtParent(string scenePath, Node parent, Vector2 globalPosition, int? childIndex = null)
    {
        if (parent == null)
        {
            MainFile.Logger.Warn("VfxUtils: Parent node is null, cannot play effect");
            return null;
        }

        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
        {
            return null;
        }

        var effect = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        if (effect == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate Node2D effect from: {scenePath}");
            return null;
        }

        if (childIndex.HasValue)
        {
            parent.AddChild(effect);
            parent.MoveChild(effect, childIndex.Value);
        }
        else
        {
            parent.AddChild(effect);
        }

        effect.GlobalPosition = globalPosition;

        MainFile.Logger.Debug($"VfxUtils: Played effect at parent node, global position {globalPosition}: {scenePath}");
        return effect;
    }

    public static void ClearCache()
    {
        SceneCache.Clear();
        MainFile.Logger.Info("VfxUtils: Scene cache cleared");
    }

    public static void PreloadScenes(params string[] scenePaths)
    {
        foreach (var path in scenePaths)
        {
            GetOrLoadScene(path);
        }
        MainFile.Logger.Info($"VfxUtils: Preloaded {scenePaths.Length} scenes");
    }
}
