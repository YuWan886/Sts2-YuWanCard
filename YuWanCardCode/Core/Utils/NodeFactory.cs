using System;
using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YuWanCard.Core.Utils;

/// <summary>
/// Simplified Godot scene factory with auto-conversion support.
/// Registered scenes are transparently converted from standard Godot types
/// (Node2D, Control) to game-specific types (NCreatureVisuals, NEnergyCounter)
/// when PackedScene.Instantiate is called.
/// </summary>
public static class NodeFactory
{
    private static readonly Dictionary<Type, string> _sceneTypePaths = new();
    private static readonly ConcurrentDictionary<string, (Type targetType, Action<Node>? postAction)> _registeredScenes = new();

    // Prevent recursive conversion when factory creates nodes
    [ThreadStatic]
    private static HashSet<Node>? _convertingNodes;

    public static void Init()
    {
        _sceneTypePaths.Clear();
        _registeredScenes.Clear();
    }

    public static void RegisterSceneType<TNode>(string path) where TNode : Node
    {
        _sceneTypePaths[typeof(TNode)] = path;
        RegisterSceneConversion<TNode>(path);
    }

    private static void RegisterSceneConversion<TNode>(string path) where TNode : Node
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!path.StartsWith("res://") && !path.StartsWith("user://"))
            path = "res://" + path;

        path = path.SimplifyPath();
        _registeredScenes[path] = (typeof(TNode), null);
    }

    public static TNode? CreateFromScene<TNode>(string path) where TNode : Node
    {
        if (!ResourceLoader.Exists(path)) return null;
        var scene = ResourceLoader.Load<PackedScene>(path);
        return scene?.Instantiate<TNode>(PackedScene.GenEditState.Disabled);
    }

    /// <summary>
    /// Called from SceneConversionPatch after non-generic Instantiate returns.
    /// If the scene path is registered and result isn't the target type,
    /// converts it in-place so the generic cast in Instantiate&lt;T&gt; succeeds.
    /// </summary>
    internal static bool TryAutoConvert(PackedScene scene, ref Node? result)
    {
        if (result == null || (_convertingNodes != null && _convertingNodes.Contains(result)))
            return false;

        var path = scene.ResourcePath.SimplifyPath();
        if (string.IsNullOrEmpty(path)) return false;
        if (!_registeredScenes.TryGetValue(path, out var sceneInfo)) return false;
        if (sceneInfo.targetType.IsInstanceOfType(result)) return false;

        _convertingNodes ??= [];
        var converting = result;
        _convertingNodes.Add(converting);

        try
        {
            var converted = ConvertNode(result, sceneInfo.targetType);
            if (converted == null) return false;

            sceneInfo.postAction?.Invoke(converted);
            result = converted;
            return true;
        }
        finally
        {
            _convertingNodes.Remove(converting);
        }
    }

    private static Node? ConvertNode(Node source, Type targetType)
    {
        if (targetType == typeof(NCreatureVisuals))
            return ConvertToNCreatureVisuals(source);
        if (targetType == typeof(NEnergyCounter))
            return ConvertToNEnergyCounter(source);
        if (targetType == typeof(NMerchantCharacter))
            return ConvertToMerchantCharacter(source);
        if (targetType == typeof(NRestSiteCharacter))
            return ConvertToSimple<Node2D, NRestSiteCharacter>(source);

        return null;
    }

    private static TTarget ConvertToSimple<TSource, TTarget>(Node source)
        where TSource : Node
        where TTarget : Node, new()
    {
        var target = new TTarget { Name = source.Name };

        if (source is CanvasItem srcCI && target is CanvasItem tgtCI)
        {
            tgtCI.Visible = srcCI.Visible;
            tgtCI.Modulate = srcCI.Modulate;
            tgtCI.SelfModulate = srcCI.SelfModulate;
        }

        if (source is Node2D src2D && target is Node2D tgt2D)
        {
            tgt2D.Position = src2D.Position;
        }

        TransferChildren(source, target);
        source.QueueFree();
        return target;
    }

    private static NMerchantCharacter ConvertToMerchantCharacter(Node source)
    {
        var target = new NMerchantCharacter { Name = source.Name };

        if (source is CanvasItem srcCI)
        {
            target.Visible = srcCI.Visible;
            target.Modulate = srcCI.Modulate;
            target.SelfModulate = srcCI.SelfModulate;
        }

        if (source is Node2D src2D)
        {
            target.Position = src2D.Position;
        }

        TransferChildrenFiltered(source, target, child => child is AnimatedSprite2D or Sprite2D);
        source.QueueFree();
        return target;
    }

    private static NCreatureVisuals ConvertToNCreatureVisuals(Node source)
    {
        var visuals = new NCreatureVisuals { Name = source.Name };

        // Copy position if source is a CanvasItem
        if (source is Node2D source2D)
            visuals.Position = source2D.Position;

        // Transfer all children and set ownership for unique name resolution
        TransferChildren(source, visuals);

        // Generate missing required nodes that NCreatureVisuals._Ready() expects
        var bounds = visuals.GetNodeOrNull<Control>("%Bounds");
        if (bounds == null)
        {
            bounds = new Control
            {
                Name = "Bounds",
                UniqueNameInOwner = true,
                Size = new Vector2(240, 280),
                Position = new Vector2(-120, -280)
            };
            visuals.AddChild(bounds);
            bounds.Owner = visuals;
        }

        EnsureMarker(visuals, "CenterPos", bounds.Position + bounds.Size * new Vector2(0.5f, 0.6f));
        EnsureMarker(visuals, "IntentPos", bounds.Position + bounds.Size * new Vector2(0.5f, 0f) + new Vector2(0, -70));
        EnsureMarker(visuals, "OrbPos", bounds.Position + bounds.Size * new Vector2(0.5f, 0f));

        // Bounds container needs a child "Bounds" Control for UpdateBounds
        if (!bounds.HasNode("Bounds"))
        {
            var innerBounds = new Control
            {
                Name = "Bounds",
                Size = bounds.Size,
                Position = Vector2.Zero
            };
            bounds.AddChild(innerBounds);
            innerBounds.Owner = visuals;
        }

        // Bounds container needs a child "IntentPos" Marker2D for intent positioning
        if (!bounds.HasNode("IntentPos"))
        {
            var intentMarker = new Marker2D
            {
                Name = "IntentPos",
                Position = new Vector2(bounds.Size.X * 0.5f, -70)
            };
            bounds.AddChild(intentMarker);
            intentMarker.Owner = visuals;
        }

        source.QueueFree();
        return visuals;
    }

    private static NEnergyCounter ConvertToNEnergyCounter(Node source)
    {
        var counter = new NEnergyCounter
        {
            Name = source.Name,
            Size = new Vector2(128f, 128f),
            PivotOffset = new Vector2(64f, 64f)
        };

        // Copy Control properties
        if (source is Control sourceControl)
        {
            counter.LayoutMode = sourceControl.LayoutMode;
            counter.AnchorsPreset = sourceControl.AnchorsPreset;
            counter.OffsetLeft = sourceControl.OffsetLeft;
            counter.OffsetTop = sourceControl.OffsetTop;
            counter.OffsetRight = sourceControl.OffsetRight;
            counter.OffsetBottom = sourceControl.OffsetBottom;
            counter.GrowHorizontal = sourceControl.GrowHorizontal;
            counter.GrowVertical = sourceControl.GrowVertical;
            counter.MouseFilter = sourceControl.MouseFilter;
            counter.FocusMode = sourceControl.FocusMode;
            counter.Modulate = sourceControl.Modulate;
            counter.SelfModulate = sourceControl.SelfModulate;
        }

        TransferChildren(source, counter);

        // Post-process: convert types that failed script resolution
        ConvertChildTypes(counter);

        source.QueueFree();
        return counter;
    }

    /// <summary>
    /// Walk the converted node tree and fix child types that failed to resolve
    /// their Godot script references (e.g. Label to MegaLabel).
    /// </summary>
    private static void ConvertChildTypes(Node root)
    {
        var replacements = new List<(Node oldNode, Node newNode)>();

        foreach (var child in GetAllChildren(root))
        {
            Node? replacement = null;

            if (child is Label label && child.GetType() == typeof(Label))
                replacement = ConvertLabelToMegaLabel(label);

            if (replacement != null)
                replacements.Add((child, replacement));
        }

        foreach (var (oldNode, newNode) in replacements)
        {
            var parent = oldNode.GetParent();
            if (parent != null)
            {
                oldNode.ReplaceBy(newNode);
                oldNode.QueueFree();
            }
        }
    }

    private static List<Node> GetAllChildren(Node root)
    {
        var result = new List<Node>();
        CollectChildren(root, result);
        return result;
    }

    private static void CollectChildren(Node node, List<Node> result)
    {
        foreach (var child in node.GetChildren())
        {
            result.Add(child);
            CollectChildren(child, result);
        }
    }

    private static MegaLabel ConvertLabelToMegaLabel(Label source)
    {
        var mega = new MegaLabel
        {
            Name = source.Name,
            Text = source.Text,
            HorizontalAlignment = source.HorizontalAlignment,
            VerticalAlignment = source.VerticalAlignment,
            AutowrapMode = source.AutowrapMode,
            ClipText = source.ClipText,
            Uppercase = source.Uppercase,
            VisibleCharactersBehavior = source.VisibleCharactersBehavior,
            AutoSizeEnabled = true,
            MinFontSize = 32,
            MaxFontSize = 36
        };

        // Copy Control properties
        CopyControlProps(source, mega);

        // Copy theme overrides
        CopyThemeOverrides(source, mega);

        return mega;
    }

    private static void CopyControlProps(Control source, Control target)
    {
        target.LayoutMode = source.LayoutMode;
        target.AnchorsPreset = source.AnchorsPreset;
        target.AnchorLeft = source.AnchorLeft;
        target.AnchorTop = source.AnchorTop;
        target.AnchorRight = source.AnchorRight;
        target.AnchorBottom = source.AnchorBottom;
        target.OffsetLeft = source.OffsetLeft;
        target.OffsetTop = source.OffsetTop;
        target.OffsetRight = source.OffsetRight;
        target.OffsetBottom = source.OffsetBottom;
        target.GrowHorizontal = source.GrowHorizontal;
        target.GrowVertical = source.GrowVertical;
        target.MouseFilter = source.MouseFilter;
        target.FocusMode = source.FocusMode;
        target.Modulate = source.Modulate;
        target.SelfModulate = source.SelfModulate;
        target.Size = source.Size;
        target.CustomMinimumSize = source.CustomMinimumSize;
        target.PivotOffset = source.PivotOffset;
        target.Rotation = source.Rotation;
        target.Scale = source.Scale;
        target.Visible = source.Visible;
        target.ZIndex = source.ZIndex;
    }

    private static void CopyThemeOverrides(Label source, Label target)
    {
        // Copy theme colors
        var fontColor = source.GetThemeColor("font_color", "Label");
        if (fontColor != default)
            target.AddThemeColorOverride("font_color", fontColor);
        var fontShadowColor = source.GetThemeColor("font_shadow_color", "Label");
        if (fontShadowColor != default)
            target.AddThemeColorOverride("font_shadow_color", fontShadowColor);
        var fontOutlineColor = source.GetThemeColor("font_outline_color", "Label");
        if (fontOutlineColor != default)
            target.AddThemeColorOverride("font_outline_color", fontOutlineColor);

        // Copy theme constants
        target.AddThemeConstantOverride("shadow_offset_x", source.GetThemeConstant("shadow_offset_x", "Label"));
        target.AddThemeConstantOverride("shadow_offset_y", source.GetThemeConstant("shadow_offset_y", "Label"));
        target.AddThemeConstantOverride("outline_size", source.GetThemeConstant("outline_size", "Label"));
        target.AddThemeConstantOverride("shadow_outline_size", source.GetThemeConstant("shadow_outline_size", "Label"));

        // Copy font
        var font = source.GetThemeFont("font", "Label");
        if (font != null)
            target.AddThemeFontOverride("font", font);

        // Copy font size
        var fontSize = source.GetThemeFontSize("font_size", "Label");
        if (fontSize > 0)
            target.AddThemeFontSizeOverride("font_size", fontSize);
    }

    private static void TransferChildren(Node source, Node target)
    {
        foreach (var child in source.GetChildren())
        {
            source.RemoveChild(child);
            target.AddChild(child);
            child.Owner = target;
            SetOwnerRecursive(target, child);
        }
    }

    private static void TransferChildrenFiltered(Node source, Node target, Func<Node, bool> predicate)
    {
        foreach (var child in source.GetChildren())
        {
            source.RemoveChild(child);
            if (predicate(child))
            {
                target.AddChild(child);
                child.Owner = target;
                SetOwnerRecursive(target, child);
            }
            else
            {
                child.QueueFree();
            }
        }
    }

    private static void SetOwnerRecursive(Node owner, Node node)
    {
        foreach (var child in node.GetChildren())
        {
            child.Owner = owner;
            SetOwnerRecursive(owner, child);
        }
    }

    private static void EnsureMarker(Node parent, string name, Vector2 position)
    {
        if (parent.HasNode($"%{name}")) return;
        var marker = new Marker2D
        {
            Name = name,
            UniqueNameInOwner = true,
            Position = position
        };
        parent.AddChild(marker);
        marker.Owner = parent;
    }

    private static void EnsureControl(Node parent, string name, bool unique)
    {
        var path = unique ? $"%{name}" : name;
        if (parent.HasNode(path)) return;
        var control = new Control
        {
            Name = name,
            UniqueNameInOwner = unique,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        parent.AddChild(control);
        control.Owner = parent;
    }
}

/// <summary>
/// Typed NodeFactory for creating nodes from registered scene paths.
/// </summary>
public static class NodeFactory<TNode> where TNode : Node
{
    public static TNode? CreateFromScene(string path)
    {
        return NodeFactory.CreateFromScene<TNode>(path);
    }
}
