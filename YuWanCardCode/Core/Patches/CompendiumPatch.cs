using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using YuWanCard.Core.Patches;

namespace YuWanCard.Patches;

/// <summary>
/// Adds custom character card pool filters to the Compendium (card library).
/// dynamically creates NCardPoolFilter nodes for each IYuWanCharacter and
/// adds them to the pool filter and character filter dictionaries.
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
static class CompendiumPatch
{
    private const string HsvShaderPath = "res://shaders/hsv.gdshader";
    private const string SelectionReticlePath = "res://scenes/ui/selection_reticle.tscn";

    private static ShaderMaterial? _cachedHsvMaterial;

    /// <summary>
    /// Adds custom pool filters after the vanilla filter setup completes.
    /// Uses Harmony's ___fieldName convention to access private fields.
    /// </summary>
    [HarmonyPostfix]
    static void AddCustomFilters(
        NCardLibrary __instance,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        // Find the last vanilla character filter to insert custom filters after
        var defectCharacter = ModelDb.Character<Defect>();
        if (!____cardPoolFilters.TryGetValue(defectCharacter, out var lastFilter))
            return;

        var updateFilter = Callable.From<NCardPoolFilter>(__instance.UpdateCardPoolFilter);
        var lastHoveredField = AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl");

        foreach (var character in ModelDbCharactersPatch.CustomCharacters)
        {
            if (character is not IYuWanCharacter) continue;

            var filter = GenerateFilter(character);

            // Insert after the last filter in the UI
            lastFilter.AddSibling(filter, forceReadableName: true);
            lastFilter = filter;

            // Map character to its filter (for compendium open during run)
            ____cardPoolFilters[character] = filter;

            // Filter predicate: card belongs to this character's card pool
            var pool = character.CardPool;
            ____poolFilters[filter] = c => pool.AllCardIds.Contains(c.Id);

            // Connect signals to match vanilla filter behavior
            filter.Connect(NCardPoolFilter.SignalName.Toggled, updateFilter);
            filter.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
                lastHoveredField.SetValue(__instance, filter)));
        }
    }

    /// <summary>
    /// Creates an NCardPoolFilter Godot node with the character's icon
    /// and a selection reticle, matching the vanilla filter structure.
    /// </summary>
    private static NCardPoolFilter GenerateFilter(CharacterModel character)
    {
        var filter = new NCardPoolFilter
        {
            Name = $"FILTER-{character.Id.Entry}",
            Size = new Vector2(64, 64),
            CustomMinimumSize = new Vector2(64, 64)
        };

        var icon = character.IconTexture;

        // Ensure HSV shader material (required by NCardPoolFilter.OnToggle)
        var hsvMaterial = GetOrCreateHsvMaterial();

        var image = new TextureRect
        {
            Name = "Image",
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(56, 56),
            Position = new Vector2(4, 4),
            Scale = new Vector2(0.9f, 0.9f),
            PivotOffset = new Vector2(28, 28),
            Material = hsvMaterial
        };

        filter.AddChild(image);
        image.Owner = filter;

        // Add selection reticle (required by NCardPoolFilter._Ready)
        var reticle = CreateReticle();
        reticle.Name = "SelectionReticle";
        reticle.UniqueNameInOwner = true;
        filter.AddChild(reticle);
        reticle.Owner = filter;

        return filter;
    }

    private static ShaderMaterial? GetOrCreateHsvMaterial()
    {
        if (_cachedHsvMaterial != null)
            return _cachedHsvMaterial;

        var shader = GD.Load<Shader>(HsvShaderPath);
        if (shader == null) return null;

        _cachedHsvMaterial = new ShaderMaterial { Shader = shader };
        _cachedHsvMaterial.SetShaderParameter("h", 1f);
        _cachedHsvMaterial.SetShaderParameter("s", 1f);
        _cachedHsvMaterial.SetShaderParameter("v", 1f);
        return _cachedHsvMaterial;
    }

    private static NSelectionReticle CreateReticle()
    {
        // Try direct load first — PreloadManager cache may not include this scene
        if (ResourceLoader.Exists(SelectionReticlePath))
        {
            var scene = ResourceLoader.Load<PackedScene>(SelectionReticlePath);
            var reticle = scene?.Instantiate<NSelectionReticle>();
            if (reticle != null) return reticle;
        }

        // Fall back to PreloadManager cache
        var cachedScene = PreloadManager.Cache.GetScene(SelectionReticlePath);
        if (cachedScene != null)
        {
            var reticle = cachedScene.Instantiate<NSelectionReticle>();
            if (reticle != null) return reticle;
        }

        // Last resort: create bare NSelectionReticle (works without visual elements)
        return new NSelectionReticle();
    }
    

    /// <summary>
    /// Shrinks filter buttons when there are too many to fit in the grid.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyAfter("BaseLib")]
    static void AdjustFilterScales(
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters)
    {
        if (____poolFilters.Count == 0) return;
        if (____poolFilters.First().Key.GetParentControl() is not GridContainer parent)
            return;

        int count = parent.GetChildCount();
        if (count <= 8) return; // No scaling needed for 8 or fewer filters

        const float baseFilterSize = 64f;
        var scale = Vector2.One;
        int columns = 4;
        float height = baseFilterSize * MathF.Ceiling(count / (float)columns);

        while (height > baseFilterSize * 3)
        {
            columns++;
            scale = Vector2.One * (4f / columns);
            height = baseFilterSize * scale.Y * MathF.Ceiling(count / (float)columns);
        }

        var imageField = AccessTools.Field(typeof(NCardPoolFilter), "_image");
        var reticleField = AccessTools.Field(typeof(NCardPoolFilter), "_controllerSelectionReticle");

        foreach (var child in parent.GetChildren())
        {
            if (child is not NCardPoolFilter f) continue;

            f.CustomMinimumSize *= scale;
            f.Size *= scale;
            f.PivotOffset *= scale;

            if (imageField.GetValue(f) is Control img)
            {
                img.CustomMinimumSize *= scale;
                img.Size *= scale;
                img.PivotOffset *= scale;
                img.Position = (f.Size - img.Size) * 0.5f;
            }

            if (reticleField.GetValue(f) is Control reticle)
            {
                reticle.CustomMinimumSize *= scale;
                reticle.Size *= scale;
                reticle.PivotOffset *= scale;
                reticle.Position *= scale;
            }
        }

        parent.Columns = columns;
    }
}
