using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.addons.mega_text;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Patches;

internal static class CharacterSkinSelectionUiHelper
{
    private const string DefaultRichTextFontPath = "res://themes/kreon_regular_shared.tres";
    private const string PanelName = "YuWanCharacterSkinPanel";
    private const string RootName = "Root";
    private const string SkinRowName = "SkinRow";
    private const string SkinLabelName = "SkinLabel";
    private const string PreviewHostName = "PreviewHost";
    private const string PreviewViewportContainerName = "PreviewViewportContainer";
    private const string PreviewViewportName = "PreviewViewport";
    private const string PreviewStageName = "PreviewStage";
    private const float PanelWidth = 205f;
    private const float PanelHeight = 195f;
    private const float PreviewWidth = 181f;
    private const float PreviewHeight = 134f;
    private const float HorizontalGap = 90f;
    private static readonly ConditionalWeakTable<PanelContainer, PreviewState> PreviewStates = [];

    private sealed class PreviewState
    {
        public CharacterModel? Character;
        public string? SkinId;
        public CreatureAnimator? Animator;
    }

    public static PanelContainer? EnsurePanel(
        Control? parent,
        Control? infoPanel,
        NAscensionPanel ascensionPanel,
        Action onPrevious,
        Action onNext)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.GetNodeOrNull<PanelContainer>(PanelName) is { } existing)
        {
            existing.Position = GetPanelPosition(infoPanel, ascensionPanel);
            return existing;
        }

        var panel = new PanelContainer
        {
            Name = PanelName,
            Position = GetPanelPosition(infoPanel, ascensionPanel),
            Size = new Vector2(PanelWidth, PanelHeight),
            MouseFilter = Control.MouseFilterEnum.Stop
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderColor = new Color(0f, 0f, 0f, 0f),
            ShadowColor = new Color(0f, 0f, 0f, 0f)
        };
        style.SetBorderWidthAll(0);
        style.SetCornerRadiusAll(0);
        style.SetContentMarginAll(0);
        style.ShadowSize = 0;
        panel.AddThemeStyleboxOverride("panel", style);

        var root = new VBoxContainer
        {
            Name = RootName,
        };
        root.AddThemeConstantOverride("separation", 4);
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(root);

        var skinRow = new HBoxContainer
        {
            Name = SkinRowName,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        skinRow.AddThemeConstantOverride("separation", 4);
        skinRow.AddChild(CreateArrowButton("<", onPrevious));
        skinRow.AddChild(CreateRichLabel(SkinLabelName, 14, HorizontalAlignment.Center, Colors.White));
        skinRow.AddChild(CreateArrowButton(">", onNext));
        root.AddChild(skinRow);
        root.AddChild(CreatePreviewViewport());

        parent.AddChild(panel);
        return panel;
    }

    public static void SyncPanel(PanelContainer? panel, CharacterModel? character, bool visible)
    {
        if (panel == null)
        {
            return;
        }

        panel.Visible = visible;
        if (!visible || character == null)
        {
            return;
        }

        YuWanCharacterSkinDefinition? selectedSkin = CharacterSkinSelectionManager.GetSelectedSkin(character);
        if (selectedSkin == null)
        {
            panel.Visible = false;
            return;
        }

        string title = new LocString("gameplay_ui", "YUWANCARD-CHARACTER_SKIN.preview_title").GetFormattedText();
        string skinName = new LocString("gameplay_ui", selectedSkin.DisplayNameLocKey).GetFormattedText();
        panel.TooltipText = title.StripBbCodeTags();
        panel.GetNode<MegaRichTextLabel>($"{RootName}/{SkinRowName}/{SkinLabelName}")
            .SetTextAutoSize(skinName.ExpandExtendedBbCode());
        EnsurePreviewMatches(panel, character, selectedSkin);
    }

    public static Vector2 GetPanelPosition(Control? infoPanel, NAscensionPanel ascensionPanel)
    {
        if (infoPanel != null && infoPanel.Size.X > 0f)
        {
            Control? panelParent = ascensionPanel.GetParent<Control>();
            if (panelParent != null)
            {
                Vector2 globalTopRight = infoPanel.GlobalPosition + new Vector2(infoPanel.Size.X, 8f);
                Vector2 localTopRight = panelParent.GetGlobalTransform().AffineInverse() * globalTopRight;
                return localTopRight + new Vector2(HorizontalGap, 0f);
            }
        }

        float offsetY = ascensionPanel.Size.Y > 0f
            ? (ascensionPanel.Size.Y - PanelHeight) * 0.5f
            : 24f;
        return ascensionPanel.Position + new Vector2(-(PanelWidth + HorizontalGap), offsetY);
    }

    private static Button CreateArrowButton(string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(28f, 0f),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand
        };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.Pressed += onPressed;
        return button;
    }

    private static MegaRichTextLabel CreateRichLabel(
        string name,
        int fontSize,
        HorizontalAlignment horizontalAlignment,
        Color fontColor)
    {
        var label = new MegaRichTextLabel
        {
            Name = name,
            BbcodeEnabled = true,
            ScrollActive = false,
            FitContent = false,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        ApplyRichTextFontOverrides(label);
        label.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.NormalFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.BoldFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.BoldItalicsFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.ItalicsFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.MonoFontSize, fontSize);
        label.AddThemeColorOverride(ThemeConstants.RichTextLabel.DefaultColor, fontColor);
        return label;
    }

    private static void ApplyRichTextFontOverrides(MegaRichTextLabel label)
    {
        Font? font = label.GetThemeDefaultFont();
        font ??= PreloadManager.Cache.GetAsset<Font>(DefaultRichTextFontPath);
        if (font == null)
        {
            return;
        }

        label.AddThemeFontOverride(ThemeConstants.RichTextLabel.NormalFont, font);
        label.AddThemeFontOverride(ThemeConstants.RichTextLabel.BoldFont, font);
        label.AddThemeFontOverride(ThemeConstants.RichTextLabel.ItalicsFont, font);
    }

    private static Control CreatePreviewViewport()
    {
        var host = new Control
        {
            Name = PreviewHostName,
            CustomMinimumSize = new Vector2(PreviewWidth, PreviewHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var viewportContainer = new SubViewportContainer
        {
            Name = PreviewViewportContainerName,
            Stretch = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(PreviewWidth, PreviewHeight)
        };
        viewportContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        host.AddChild(viewportContainer);

        var viewport = new SubViewport
        {
            Name = PreviewViewportName,
            Disable3D = true,
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Size = new Vector2I((int)PreviewWidth, (int)PreviewHeight)
        };
        viewportContainer.AddChild(viewport);

        var stage = new Node2D
        {
            Name = PreviewStageName
        };
        viewport.AddChild(stage);

        return host;
    }

    private static void EnsurePreviewMatches(
        PanelContainer panel,
        CharacterModel character,
        YuWanCharacterSkinDefinition selectedSkin)
    {
        PreviewState state = PreviewStates.GetOrCreateValue(panel);
        if (ReferenceEquals(state.Character, character)
            && string.Equals(state.SkinId, selectedSkin.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SubViewport? viewport = panel.GetNodeOrNull<SubViewport>(
            $"{RootName}/{PreviewHostName}/{PreviewViewportContainerName}/{PreviewViewportName}");
        Node2D? stage = viewport?.GetNodeOrNull<Node2D>(PreviewStageName);
        if (viewport == null || stage == null)
        {
            return;
        }

        foreach (Node child in stage.GetChildren())
        {
            stage.RemoveChild(child);
            child.QueueFree();
        }

        state.Character = character;
        state.SkinId = selectedSkin.Id;
        state.Animator = null;

        NCreatureVisuals? previewVisuals = CreatePreviewVisuals(character);
        if (previewVisuals == null)
        {
            return;
        }

        stage.AddChild(previewVisuals);
        LayoutPreviewVisuals(previewVisuals, viewport.Size);

        if (character is IYuWanCharacter yuWanCharacter
            && previewVisuals.GetNodeOrNull("%Visuals") is Node visualsNode)
        {
            var controller = new MegaSprite(visualsNode);
            state.Animator = yuWanCharacter.SetupCustomAnimationStates(controller);
            if (state.Animator != null && state.Animator.HasTrigger(CreatureAnimator.idleTrigger))
            {
                state.Animator.SetTrigger(CreatureAnimator.idleTrigger);
            }
        }
    }

    private static NCreatureVisuals? CreatePreviewVisuals(CharacterModel character)
        => character is IYuWanCharacter yuWanCharacter
            ? yuWanCharacter.CreateCustomVisuals()
            : null;

    private static void LayoutPreviewVisuals(NCreatureVisuals previewVisuals, Vector2I viewportSize)
    {
        Vector2 scale = Vector2.One * 0.45f;
        previewVisuals.Scale = scale;

        if (previewVisuals.GetNodeOrNull<Control>("%Bounds") is { } bounds)
        {
            Vector2 boundsCenter = new(
                (bounds.OffsetLeft + bounds.OffsetRight) * 0.5f,
                (bounds.OffsetTop + bounds.OffsetBottom) * 0.5f);
            Vector2 desiredCenter = new(viewportSize.X * 0.52f, viewportSize.Y * 0.7f);
            previewVisuals.Position = desiredCenter - (boundsCenter * scale);
            return;
        }

        previewVisuals.Position = new Vector2(viewportSize.X * 0.52f, viewportSize.Y * 0.7f);
    }
}

internal static class CharacterSkinSelectScreenHelper
{
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, NCharacterSelectButton?> SelectedButtonRef =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, NCharacterSelectButton?>("_selectedButton");

    public static void EnsurePanelExists(NCharacterSelectScreen screen)
    {
        NAscensionPanel? ascensionPanel = screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        Control? infoPanel = screen.GetNodeOrNull<Control>("InfoPanel");
        Control? parent = ascensionPanel?.GetParent<Control>();
        if (ascensionPanel == null || parent == null)
        {
            return;
        }

        CharacterSkinSelectionUiHelper.EnsurePanel(
            parent,
            infoPanel,
            ascensionPanel,
            () => ChangeSkin(screen, -1),
            () => ChangeSkin(screen, 1));
    }

    public static void SyncPanel(NCharacterSelectScreen screen, CharacterModel? selectedCharacter = null)
    {
        NAscensionPanel? ascensionPanel = screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        Control? infoPanel = screen.GetNodeOrNull<Control>("InfoPanel");
        Control? parent = ascensionPanel?.GetParent<Control>();
        PanelContainer? panel = parent?.GetNodeOrNull<PanelContainer>("YuWanCharacterSkinPanel");
        if (panel == null || ascensionPanel == null)
        {
            return;
        }

        panel.Position = CharacterSkinSelectionUiHelper.GetPanelPosition(infoPanel, ascensionPanel);
        CharacterModel? character = selectedCharacter
            ?? SelectedButtonRef(screen)?.Character
            ?? screen.Lobby?.LocalPlayer.character;
        bool visible = screen.Lobby?.NetService.Type == NetGameType.Singleplayer
            && character != null
            && CharacterSkinSelectionManager.HasSelectableSkins(character);
        CharacterSkinSelectionUiHelper.SyncPanel(panel, character, visible);
    }

    private static void ChangeSkin(NCharacterSelectScreen screen, int delta)
    {
        if (screen.Lobby?.NetService.Type != NetGameType.Singleplayer)
        {
            return;
        }

        NCharacterSelectButton? selectedButton = SelectedButtonRef(screen);
        CharacterModel? character = selectedButton?.Character;
        if (selectedButton == null || character == null || !CharacterSkinSelectionManager.TryCycleSkin(character, delta))
        {
            return;
        }

        SyncPanel(screen, character);
    }
}

internal static class CharacterSkinCustomRunHelper
{
    private static readonly AccessTools.FieldRef<NCustomRunScreen, NCharacterSelectButton?> SelectedButtonRef =
        AccessTools.FieldRefAccess<NCustomRunScreen, NCharacterSelectButton?>("_selectedButton");

    public static void EnsurePanelExists(NCustomRunScreen screen)
    {
        NAscensionPanel? ascensionPanel = screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        Control? parent = ascensionPanel?.GetParent<Control>();
        if (ascensionPanel == null || parent == null)
        {
            return;
        }

        CharacterSkinSelectionUiHelper.EnsurePanel(
            parent,
            null,
            ascensionPanel,
            () => ChangeSkin(screen, -1),
            () => ChangeSkin(screen, 1));
    }

    public static void SyncPanel(NCustomRunScreen screen, CharacterModel? selectedCharacter = null)
    {
        NAscensionPanel? ascensionPanel = screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        Control? parent = ascensionPanel?.GetParent<Control>();
        PanelContainer? panel = parent?.GetNodeOrNull<PanelContainer>("YuWanCharacterSkinPanel");
        if (panel == null || ascensionPanel == null)
        {
            return;
        }

        panel.Position = CharacterSkinSelectionUiHelper.GetPanelPosition(null, ascensionPanel);
        CharacterModel? character = selectedCharacter
            ?? SelectedButtonRef(screen)?.Character
            ?? screen.Lobby?.LocalPlayer.character;
        bool visible = screen.Lobby?.NetService.Type == NetGameType.Singleplayer
            && character != null
            && CharacterSkinSelectionManager.HasSelectableSkins(character);
        CharacterSkinSelectionUiHelper.SyncPanel(panel, character, visible);
    }

    private static void ChangeSkin(NCustomRunScreen screen, int delta)
    {
        if (screen.Lobby?.NetService.Type != NetGameType.Singleplayer)
        {
            return;
        }

        NCharacterSelectButton? selectedButton = SelectedButtonRef(screen);
        CharacterModel? character = selectedButton?.Character;
        if (selectedButton == null || character == null || !CharacterSkinSelectionManager.TryCycleSkin(character, delta))
        {
            return;
        }

        SyncPanel(screen, character);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
public static class CharacterSkinSelectScreenReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        CharacterSkinSelectScreenHelper.EnsurePanelExists(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class CharacterSkinSelectScreenSelectPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
    {
        CharacterSkinSelectScreenHelper.SyncPanel(__instance, characterModel);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened))]
public static class CharacterSkinSelectScreenOpenedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        CharacterSkinSelectScreenHelper.SyncPanel(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen._Ready))]
public static class CharacterSkinCustomRunReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        CharacterSkinCustomRunHelper.EnsurePanelExists(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.SelectCharacter))]
public static class CharacterSkinCustomRunSelectPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance, CharacterModel characterModel)
    {
        CharacterSkinCustomRunHelper.SyncPanel(__instance, characterModel);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.OnSubmenuOpened))]
public static class CharacterSkinCustomRunOpenedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        CharacterSkinCustomRunHelper.SyncPanel(__instance);
    }
}
