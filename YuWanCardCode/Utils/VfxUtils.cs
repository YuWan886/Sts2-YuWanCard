using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class VfxUtils
{
    private static readonly ConcurrentDictionary<string, PackedScene> SceneCache = new();
    private static readonly ConcurrentDictionary<string, Texture2D[]> FrameCache = new();
    private static readonly ConcurrentDictionary<string, Texture2D> TextureCache = new();
    private const string StaticVfxTexturePathPrefix = "res://YuWanCard/images/vfx/vfx_";
    private const float DefaultStaticImageSize = 256f * 0.6f;
    private const float DefaultStaticImageDuration = 1.5f;
    private const float CreatureTopTextureYOffset = 20f;

    public static IReadOnlyList<Texture2D>? GetCachedFrames(string framePathPrefix)
    {
        if (FrameCache.TryGetValue(framePathPrefix, out var frames))
        {
            return frames;
        }
        return null;
    }

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

    private static Texture2D? GetOrLoadTexture(string texturePath)
    {
        if (TextureCache.TryGetValue(texturePath, out var cachedTexture))
        {
            return cachedTexture;
        }

        var texture = ResourceLoader.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            MainFile.Logger.Warn($"VfxUtils: Failed to load texture: {texturePath}");
            return null;
        }

        TextureCache[texturePath] = texture;
        return texture;
    }

    private static float GetStaticImageScale(Texture2D texture)
    {
        var size = texture.GetSize();
        var maxDimension = Mathf.Max(size.X, size.Y);
        if (maxDimension <= 0)
        {
            return 0.6f;
        }

        return DefaultStaticImageSize / maxDimension;
    }

    private static string GetStaticVfxTexturePath(string vfxName)
    {
        return $"{StaticVfxTexturePathPrefix}{vfxName}.png";
    }

    private static string GetStaticVfxTexturePathFromCaller([CallerFilePath] string callerFilePath = "")
    {
        var fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        return GetStaticVfxTexturePath(ToSnakeCase(fileName));
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            if (char.IsUpper(ch))
            {
                if (builder.Length > 0 && builder[^1] != '_')
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        return builder.ToString().Trim('_');
    }

    private static void ScheduleAutoFree(Node node, float? durationSeconds)
    {
        var duration = durationSeconds ?? DefaultStaticImageDuration;
        if (duration <= 0)
        {
            return;
        }

        var tree = node.GetTree();
        if (tree == null)
        {
            return;
        }

        tree.CreateTimer(duration).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(node))
            {
                node.QueueFree();
            }
        };
    }

    private static Sprite2D? SpawnStaticImage(Texture2D texture, float? durationSeconds)
    {
        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer == null)
        {
            MainFile.Logger.Warn("VfxUtils: CombatVfxContainer not found, cannot play texture");
            return null;
        }

        var sprite = new Sprite2D
        {
            Texture = texture,
            Centered = true,
            ZIndex = 100
        };

        var scale = GetStaticImageScale(texture);
        sprite.Scale = new Vector2(scale, scale);

        vfxContainer.AddChildSafely(sprite);
        ScheduleAutoFree(sprite, durationSeconds);
        return sprite;
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

        var instance = scene.Instantiate(PackedScene.GenEditState.Disabled);
        if (instance == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate effect from: {scenePath}");
            return null;
        }

        vfxContainer.AddChildSafely(instance);

        if (instance is Control control)
        {
            var viewportSize = NGame.Instance?.GetViewportRect().Size ?? Vector2.Zero;
            control.Position = viewportSize * 0.5f - control.Size * 0.5f;
        }
        else if (instance is Node2D node2D)
        {
            var viewportSize = NGame.Instance?.GetViewportRect().Size ?? Vector2.Zero;
            node2D.Position = viewportSize * 0.5f;
        }

        if (instance is Node node)
        {
            var animatedSprite = node.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite?.SpriteFrames != null)
            {
                var animNames = animatedSprite.SpriteFrames.GetAnimationNames();
                if (animNames.Length > 0)
                {
                    animatedSprite.Play(animNames[0]);
                }
            }
        }

        MainFile.Logger.Debug($"VfxUtils: Played centered effect: {scenePath}");
        return instance as Control;
    }

    public static Sprite2D? PlayTextureCentered(string texturePath, float? durationSeconds = null)
    {
        var texture = GetOrLoadTexture(texturePath);
        if (texture == null)
        {
            return null;
        }

        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        var viewportSize = NGame.Instance?.GetViewportRect().Size ?? Vector2.Zero;
        sprite.Position = viewportSize * 0.5f;

        MainFile.Logger.Debug($"VfxUtils: Played centered texture: {texturePath}");
        return sprite;
    }

    public static Sprite2D? PlayTextureCentered(Texture2D texture, float? durationSeconds = null)
    {
        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        var viewportSize = NGame.Instance?.GetViewportRect().Size ?? Vector2.Zero;
        sprite.Position = viewportSize * 0.5f;
        MainFile.Logger.Debug("VfxUtils: Played centered texture instance");
        return sprite;
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

        var instance = scene.Instantiate(PackedScene.GenEditState.Disabled);
        if (instance == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate effect from: {scenePath}");
            return null;
        }

        vfxContainer.AddChildSafely(instance);

        if (instance is Control control)
        {
            control.Position = position - control.Size * 0.5f;
        }
        else if (instance is Node2D node2D)
        {
            node2D.Position = position;
        }

        MainFile.Logger.Debug($"VfxUtils: Played effect at position {position}: {scenePath}");
        return instance as Control;
    }

    public static Sprite2D? PlayTextureAt(string texturePath, Vector2 position, float? durationSeconds = null)
    {
        var texture = GetOrLoadTexture(texturePath);
        if (texture == null)
        {
            return null;
        }

        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        sprite.GlobalPosition = position;
        MainFile.Logger.Debug($"VfxUtils: Played texture at position {position}: {texturePath}");
        return sprite;
    }

    public static Sprite2D? PlayTextureAt(Texture2D texture, Vector2 position, float? durationSeconds = null)
    {
        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        sprite.GlobalPosition = position;
        MainFile.Logger.Debug($"VfxUtils: Played texture at position {position}");
        return sprite;
    }

    public static Node2D? PlayAtCreature(string scenePath, Creature creature)
    {
        return PlayAtCreatureInternal(scenePath, creature, null);
    }

    public static Node2D? PlayAtCreature(string scenePath, Creature creature, float durationSeconds)
    {
        return PlayAtCreatureInternal(scenePath, creature, durationSeconds);
    }

    private static Node2D? PlayAtCreatureInternal(string scenePath, Creature creature, float? durationSeconds)
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

        var globalPos = creatureNode.GetBottomOfHitbox();

        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
        {
            return null;
        }

        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer == null)
        {
            MainFile.Logger.Warn("VfxUtils: CombatVfxContainer not found, cannot play effect at creature position");
            return null;
        }

        var instance = scene.Instantiate(PackedScene.GenEditState.Disabled);
        if (instance == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate effect from: {scenePath}");
            return null;
        }

        vfxContainer.AddChildSafely(instance);

        if (instance is Control control)
        {
            control.GlobalPosition = globalPos;
            control.Position -= new Vector2(control.Size.X * 0.5f, control.Size.Y);
        }
        else if (instance is Node2D node2D)
        {
            node2D.GlobalPosition = globalPos;
            var animatedSprite = node2D.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite != null && animatedSprite.SpriteFrames != null)
            {
                var texture = animatedSprite.SpriteFrames.GetFrameTexture("default", 0);
                if (texture != null)
                {
                    node2D.GlobalPosition -= new Vector2(0, texture.GetHeight() * 0.5f);
                }
            }
        }

        if (instance is Node node)
        {
            var animatedSprite = node.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite != null && animatedSprite.SpriteFrames != null)
            {
                var animNames = animatedSprite.SpriteFrames.GetAnimationNames();
                if (animNames.Length > 0)
                {
                    animatedSprite.Play(animNames[0]);
                    if (durationSeconds.HasValue)
                    {
                        var tree = node.GetTree();
                        if (tree != null)
                        {
                            tree.CreateTimer(durationSeconds.Value).Timeout += () =>
                            {
                                animatedSprite.Stop();
                                node.QueueFree();
                            };
                        }
                    }
                    else
                    {
                        animatedSprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished,
                            Callable.From(() => node.QueueFree()));
                    }
                }
            }
        }

        MainFile.Logger.Debug($"VfxUtils: Played effect at creature position {globalPos}: {scenePath}");
        return instance as Node2D;
    }

    public static Sprite2D? PlayTextureAtCreature(string texturePath, Creature creature, float? durationSeconds = null)
    {
        if (creature == null)
        {
            MainFile.Logger.Warn("VfxUtils: Creature is null, cannot play texture at creature position");
            return null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            MainFile.Logger.Warn("VfxUtils: Could not get creature node for creature");
            return null;
        }

        var texture = GetOrLoadTexture(texturePath);
        if (texture == null)
        {
            return null;
        }

        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        var globalPos = creatureNode.GetBottomOfHitbox();
        var scale = GetStaticImageScale(texture);
        var scaledHeight = texture.GetSize().Y * scale;
        sprite.GlobalPosition = globalPos - new Vector2(0, scaledHeight * 0.5f);

        MainFile.Logger.Debug($"VfxUtils: Played texture at creature position {globalPos}: {texturePath}");
        return sprite;
    }

    public static Sprite2D? PlayTextureAtCreature(Texture2D texture, Creature creature, float? durationSeconds = null)
    {
        if (creature == null)
        {
            MainFile.Logger.Warn("VfxUtils: Creature is null, cannot play texture at creature position");
            return null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            MainFile.Logger.Warn("VfxUtils: Could not get creature node for creature");
            return null;
        }

        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        var globalPos = creatureNode.GetBottomOfHitbox();
        var scale = GetStaticImageScale(texture);
        var scaledHeight = texture.GetSize().Y * scale;
        sprite.GlobalPosition = globalPos - new Vector2(0, scaledHeight * 0.5f);

        MainFile.Logger.Debug($"VfxUtils: Played texture at creature position {globalPos}");
        return sprite;
    }

    public static Node2D? PlayAtCreatureTop(string scenePath, Creature creature)
    {
        return PlayAtCreatureTopInternal(scenePath, creature, null);
    }

    public static Node2D? PlayAtCreatureTop(string scenePath, Creature creature, float durationSeconds)
    {
        return PlayAtCreatureTopInternal(scenePath, creature, durationSeconds);
    }

    private static Node2D? PlayAtCreatureTopInternal(string scenePath, Creature creature, float? durationSeconds)
    {
        if (creature == null)
        {
            MainFile.Logger.Warn("VfxUtils: Creature is null, cannot play effect at creature top");
            return null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            MainFile.Logger.Warn("VfxUtils: Could not get creature node for creature");
            return null;
        }

        var topPos = creatureNode.GetTopOfHitbox();

        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
        {
            return null;
        }

        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer == null)
        {
            MainFile.Logger.Warn("VfxUtils: CombatVfxContainer not found, cannot play effect at creature top");
            return null;
        }

        var instance = scene.Instantiate(PackedScene.GenEditState.Disabled);
        if (instance == null)
        {
            MainFile.Logger.Error($"VfxUtils: Failed to instantiate effect from: {scenePath}");
            return null;
        }

        vfxContainer.AddChildSafely(instance);

        if (instance is Control control)
        {
            control.GlobalPosition = topPos;
            control.Position -= new Vector2(control.Size.X * 0.5f, control.Size.Y);
        }
        else if (instance is Node2D node2D)
        {
            var animatedSprite = node2D.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite != null && animatedSprite.SpriteFrames != null)
            {
                var texture = animatedSprite.SpriteFrames.GetFrameTexture("default", 0);
                if (texture != null)
                {
                    node2D.GlobalPosition = topPos;
                    node2D.GlobalPosition -= new Vector2(0, texture.GetHeight() * 0.5f);
                }
            }
            else
            {
                node2D.GlobalPosition = topPos;
            }
        }

        if (instance is Node node)
        {
            var animatedSprite = node.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite != null && animatedSprite.SpriteFrames != null)
            {
                var animNames = animatedSprite.SpriteFrames.GetAnimationNames();
                if (animNames.Length > 0)
                {
                    animatedSprite.Play(animNames[0]);
                    if (durationSeconds.HasValue)
                    {
                        var tree = node.GetTree();
                        if (tree != null)
                        {
                            tree.CreateTimer(durationSeconds.Value).Timeout += () =>
                            {
                                animatedSprite.Stop();
                                node.QueueFree();
                            };
                        }
                    }
                    else
                    {
                        animatedSprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished,
                            Callable.From(() => node.QueueFree()));
                    }
                }
            }
        }

        MainFile.Logger.Debug($"VfxUtils: Played effect at creature top {topPos}: {scenePath}");
        return instance as Node2D;
    }

    public static Sprite2D? PlayTextureAtCreatureTop(string texturePath, Creature creature, float? durationSeconds = null)
    {
        if (creature == null)
        {
            MainFile.Logger.Warn("VfxUtils: Creature is null, cannot play texture at creature top");
            return null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            MainFile.Logger.Warn("VfxUtils: Could not get creature node for creature");
            return null;
        }

        var texture = GetOrLoadTexture(texturePath);
        if (texture == null)
        {
            return null;
        }

        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        var topPos = creatureNode.GetTopOfHitbox();
        var scale = GetStaticImageScale(texture);
        var scaledHeight = texture.GetSize().Y * scale;
        sprite.GlobalPosition = topPos - new Vector2(0, scaledHeight * 0.5f + CreatureTopTextureYOffset);

        MainFile.Logger.Debug($"VfxUtils: Played texture at creature top {topPos}: {texturePath}");
        return sprite;
    }

    public static Sprite2D? PlayStaticVfxAtCreatureTop(string vfxName, Creature creature, float? durationSeconds = null)
    {
        return PlayTextureAtCreatureTop(GetStaticVfxTexturePath(vfxName), creature, durationSeconds);
    }

    public static Sprite2D? PlayStaticVfxAtCreatureTop(Creature creature, float? durationSeconds = null, [CallerFilePath] string callerFilePath = "")
    {
        return PlayTextureAtCreatureTop(GetStaticVfxTexturePathFromCaller(callerFilePath), creature, durationSeconds);
    }

    public static Sprite2D? PlayTextureAtCreatureTop(Texture2D texture, Creature creature, float? durationSeconds = null)
    {
        if (creature == null)
        {
            MainFile.Logger.Warn("VfxUtils: Creature is null, cannot play texture at creature top");
            return null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            MainFile.Logger.Warn("VfxUtils: Could not get creature node for creature");
            return null;
        }

        var sprite = SpawnStaticImage(texture, durationSeconds);
        if (sprite == null)
        {
            return null;
        }

        var topPos = creatureNode.GetTopOfHitbox();
        var scale = GetStaticImageScale(texture);
        var scaledHeight = texture.GetSize().Y * scale;
        sprite.GlobalPosition = topPos - new Vector2(0, scaledHeight * 0.5f + CreatureTopTextureYOffset);

        MainFile.Logger.Debug($"VfxUtils: Played texture at creature top {topPos}");
        return sprite;
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
        FrameCache.Clear();
        TextureCache.Clear();
        MainFile.Logger.Info("VfxUtils: Scene cache cleared");
    }

    public static void PreloadScenes(params string[] scenePaths)
    {
        foreach (var path in scenePaths)
        {
            GetOrLoadScene(path);
        }
        MainFile.Logger.Debug($"VfxUtils: Preloaded {scenePaths.Length} scenes");
    }

    public static void PreloadFrames(string framePathPrefix, int totalFrames)
    {
        var frames = new Texture2D[totalFrames];
        int loadedCount = 0;

        for (int i = 1; i <= totalFrames; i++)
        {
            var framePath = $"{framePathPrefix}_{i}.png";
            var texture = ResourceLoader.Load<Texture2D>(framePath);
            if (texture != null)
            {
                frames[i - 1] = texture;
                loadedCount++;
            }
            else
            {
                MainFile.Logger.Warn($"VfxUtils: Failed to load frame: {framePath}");
            }
        }

        if (loadedCount > 0)
        {
            FrameCache[framePathPrefix] = frames;
            MainFile.Logger.Debug($"VfxUtils: Preloaded {loadedCount}/{totalFrames} frames for {framePathPrefix}");
        }
    }

    public static void PreloadTextures(params string[] texturePaths)
    {
        var loadedCount = 0;
        foreach (var path in texturePaths)
        {
            if (GetOrLoadTexture(path) != null)
            {
                loadedCount++;
            }
        }

        MainFile.Logger.Debug($"VfxUtils: Preloaded {loadedCount}/{texturePaths.Length} textures");
    }
}
