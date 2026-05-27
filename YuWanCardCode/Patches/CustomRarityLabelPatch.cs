using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.addons.mega_text;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Patches;

/// <summary>
/// Postfixes the relic inspect screen to support <see cref="YuWanRelicModel.CustomRarityLabelKey"/>
/// for displaying a custom rarity label instead of the standard "RELIC_RARITY.{enum}" lookup.
/// </summary>
[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
public static class CustomRarityLabelPatch
{
    private static readonly StringName LabelThemeType = new("Label");
    private static readonly StringName MegaLabelThemeType = new("MegaLabel");
    private static readonly StringName RichTextThemeType = new("RichTextLabel");
    private static readonly FieldInfo? _relicsField = AccessTools.Field(typeof(NInspectRelicScreen), "_relics");
    private static readonly FieldInfo? _indexField = AccessTools.Field(typeof(NInspectRelicScreen), "_index");
    private static readonly FieldInfo? _rarityLabelField = AccessTools.Field(typeof(NInspectRelicScreen), "_rarityLabel");

    public static void Postfix(NInspectRelicScreen __instance)
    {
        if (_relicsField == null || _indexField == null || _rarityLabelField == null)
            return;

        if (_relicsField.GetValue(__instance) is not IReadOnlyList<RelicModel> relics)
            return;

        var indexObj = _indexField.GetValue(__instance);
        if (indexObj is not int index || index < 0 || index >= relics.Count)
            return;

        var relic = relics[index];
        var customKey = (relic as YuWanRelicModel)?.CustomRarityLabelKey;
        if (string.IsNullOrEmpty(customKey))
            return;

        var rarityLabel = _rarityLabelField.GetValue(__instance);
        if (rarityLabel == null)
            return;

        // The original sets empty string for locked/undiscovered states;
        // only apply custom label when text was actually shown.
        var currentText = rarityLabel.GetType().GetProperty("Text")?.GetValue(rarityLabel) as string;
        if (string.IsNullOrEmpty(currentText))
            return;

        ReplaceWithRichTextLabel(__instance, rarityLabel, new LocString("relics", customKey).GetFormattedText());
    }

    private static void ReplaceWithRichTextLabel(NInspectRelicScreen screen, object rarityLabel, string text)
    {
        if (rarityLabel is not MegaLabel source)
            return;

        var parent = source.GetParent();
        if (parent == null)
            return;

        var rich = parent.GetNodeOrNull<MegaRichTextLabel>("__YwRarityRichText");
        if (rich == null)
        {
            rich = new MegaRichTextLabel
            {
                Name = "__YwRarityRichText",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                BbcodeEnabled = true,
                ScrollActive = false,
                AutowrapMode = TextServer.AutowrapMode.Off,
                FitContent = false
            };
            parent.AddChild(rich);
            parent.MoveChild(rich, source.GetIndex() + 1);
            rich.Owner = parent;
        }

        SyncTheme(source, rich);
        rich.Visible = true;
        rich.Modulate = source.Modulate;
        rich.SelfModulate = source.SelfModulate;
        rich.AutoSizeEnabled = source.AutoSizeEnabled;
        rich.MinFontSize = source.MinFontSize;
        rich.MaxFontSize = source.MaxFontSize;
        rich.LayoutMode = source.LayoutMode;
        rich.AnchorsPreset = source.AnchorsPreset;
        rich.AnchorLeft = source.AnchorLeft;
        rich.AnchorTop = source.AnchorTop;
        rich.AnchorRight = source.AnchorRight;
        rich.AnchorBottom = source.AnchorBottom;
        rich.OffsetLeft = source.OffsetLeft;
        rich.OffsetTop = source.OffsetTop;
        rich.OffsetRight = source.OffsetRight;
        rich.OffsetBottom = source.OffsetBottom;
        rich.GrowHorizontal = source.GrowHorizontal;
        rich.GrowVertical = source.GrowVertical;
        rich.Size = source.Size;
        rich.CustomMinimumSize = source.CustomMinimumSize;
        rich.PivotOffset = source.PivotOffset;
        rich.Rotation = source.Rotation;
        rich.Scale = source.Scale;
        rich.ZIndex = source.ZIndex + 1;
        rich.HorizontalAlignment = source.HorizontalAlignment;
        rich.VerticalAlignment = source.VerticalAlignment;
        rich.SetTextAutoSize(text);
        source.Visible = false;
    }

    private static void SyncTheme(MegaLabel source, MegaRichTextLabel target)
    {
        var font = source.GetThemeFont(ThemeConstants.Label.Font, LabelThemeType)
                   ?? source.GetThemeFont(ThemeConstants.Label.Font, MegaLabelThemeType);
        if (font != null)
        {
            target.AddThemeFontOverride(ThemeConstants.RichTextLabel.NormalFont, font);
            target.AddThemeFontOverride(ThemeConstants.RichTextLabel.BoldFont, font);
            target.AddThemeFontOverride(ThemeConstants.RichTextLabel.ItalicsFont, font);
        }

        var size = source.GetThemeFontSize(ThemeConstants.Label.FontSize, LabelThemeType);
        if (size <= 0)
            size = source.GetThemeFontSize(ThemeConstants.Label.FontSize, MegaLabelThemeType);
        if (size > 0)
        {
            target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.NormalFontSize, size);
            target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.BoldFontSize, size);
            target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.BoldItalicsFontSize, size);
            target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.ItalicsFontSize, size);
            target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.MonoFontSize, size);
        }

        var color = source.GetThemeColor(ThemeConstants.Label.FontColor, LabelThemeType);
        if (color.A <= 0f)
            color = source.GetThemeColor(ThemeConstants.Label.FontColor, MegaLabelThemeType);
        if (color.A > 0f)
            target.AddThemeColorOverride(ThemeConstants.RichTextLabel.DefaultColor, color);

        var outlineColor = source.GetThemeColor(ThemeConstants.Label.FontOutlineColor, LabelThemeType);
        if (outlineColor.A > 0f)
            target.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontOutlineColor, outlineColor);

        var shadowColor = source.GetThemeColor(ThemeConstants.Label.FontShadowColor, LabelThemeType);
        if (shadowColor.A > 0f)
            target.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontShadowColor, shadowColor);

        var outlineSize = source.GetThemeConstant(ThemeConstants.Label.OutlineSize, LabelThemeType);
        if (outlineSize <= 0)
            outlineSize = source.GetThemeConstant(ThemeConstants.Label.OutlineSize, MegaLabelThemeType);
        if (outlineSize > 0)
            target.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, outlineSize);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NInspectRelicScreen), "Close")]
    private static void CleanupProxy(NInspectRelicScreen __instance)
    {
        var label = _rarityLabelField?.GetValue(__instance) as MegaLabel;
        var parent = label?.GetParent();
        var rich = parent?.GetNodeOrNull<MegaRichTextLabel>("__YwRarityRichText");
        rich?.QueueFree();
        if (label != null)
            label.Visible = true;
    }
}
