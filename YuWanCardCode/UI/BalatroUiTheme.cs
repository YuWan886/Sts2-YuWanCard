using Godot;
using YuWanCard.Balatro;

namespace YuWanCard.UI;

internal static class BalatroUiTheme
{
    public static readonly Color Surface = new(0.1f, 0.095f, 0.09f, 0.96f);
    public static readonly Color SurfaceAlt = new(0.14f, 0.13f, 0.12f, 0.98f);
    public static readonly Color SurfaceHover = new(0.17f, 0.155f, 0.14f, 0.98f);
    public static readonly Color SurfacePressed = new(0.08f, 0.075f, 0.07f, 0.98f);
    public static readonly Color SurfaceDisabled = new(0.09f, 0.09f, 0.09f, 0.58f);
    public static readonly Color Border = new(0.73f, 0.67f, 0.56f, 0.94f);
    public static readonly Color BorderStrong = new(0.93f, 0.86f, 0.71f, 0.98f);
    public static readonly Color Title = new(0.97f, 0.93f, 0.86f);
    public static readonly Color Body = new(0.87f, 0.85f, 0.81f);
    public static readonly Color Muted = new(0.66f, 0.64f, 0.6f);
    public static readonly Color Accent = new(0.91f, 0.82f, 0.63f);
    public static readonly Color Price = new(0.95f, 0.84f, 0.44f);

    public static StyleBoxFlat CreatePanelStyle()
    {
        return CreateBox(
            Surface,
            BorderStrong,
            borderWidth: 2,
            radius: 12,
            shadowColor: new Color(0f, 0f, 0f, 0.24f),
            shadowSize: 4);
    }

    public static StyleBoxFlat CreateCardStyle(Color? background = null, Color? borderColor = null)
    {
        return CreateBox(
            background ?? SurfaceAlt,
            borderColor ?? Border,
            borderWidth: 1,
            radius: 10,
            shadowColor: new Color(0f, 0f, 0f, 0.18f),
            shadowSize: 2);
    }

    public static void ApplyCardButtonStyle(Button button)
    {
        button.Text = string.Empty;
        button.AddThemeStyleboxOverride("normal", CreateCardStyle());
        button.AddThemeStyleboxOverride("hover", CreateCardStyle(SurfaceHover, BorderStrong));
        button.AddThemeStyleboxOverride("pressed", CreateCardStyle(SurfacePressed, BorderStrong));
        button.AddThemeStyleboxOverride("disabled", CreateCardStyle(SurfaceDisabled, new Color(Border.R, Border.G, Border.B, 0.4f)));
        button.AddThemeColorOverride("font_color", Title);
        button.AddThemeColorOverride("font_hover_color", Title);
        button.AddThemeColorOverride("font_pressed_color", Title);
        button.AddThemeColorOverride("font_disabled_color", new Color(Muted.R, Muted.G, Muted.B, 0.7f));
    }

    public static void ApplyActionButtonStyle(Button button, bool primary = false)
    {
        Color normal = primary ? new Color(0.22f, 0.18f, 0.12f, 0.98f) : SurfaceAlt;
        Color hover = primary ? new Color(0.28f, 0.22f, 0.15f, 0.98f) : SurfaceHover;
        Color pressed = primary ? new Color(0.17f, 0.14f, 0.1f, 0.98f) : SurfacePressed;
        Color border = primary ? BorderStrong : Border;
        Color fontColor = primary ? Title : Body;

        button.AddThemeStyleboxOverride("normal", CreateBox(normal, border, 1, 10));
        button.AddThemeStyleboxOverride("hover", CreateBox(hover, BorderStrong, 1, 10));
        button.AddThemeStyleboxOverride("pressed", CreateBox(pressed, BorderStrong, 1, 10));
        button.AddThemeStyleboxOverride("disabled", CreateBox(SurfaceDisabled, new Color(border.R, border.G, border.B, 0.35f), 1, 10));
        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_hover_color", Title);
        button.AddThemeColorOverride("font_pressed_color", Title);
        button.AddThemeColorOverride("font_disabled_color", new Color(Muted.R, Muted.G, Muted.B, 0.75f));
    }

    public static void ApplySlotButtonStyle(Button button, bool selected, bool unlocked)
    {
        Color background = unlocked
            ? selected ? new Color(0.2f, 0.17f, 0.12f, 0.98f) : new Color(0.13f, 0.12f, 0.11f, 0.95f)
            : new Color(0.09f, 0.09f, 0.09f, 0.72f);
        Color border = unlocked
            ? selected ? BorderStrong : Border
            : new Color(0.34f, 0.34f, 0.34f, 0.75f);

        button.AddThemeStyleboxOverride("normal", CreateBox(background, border, selected ? 2 : 1, 10));
        button.AddThemeStyleboxOverride("hover", CreateBox(SurfaceHover, BorderStrong, 2, 10));
        button.AddThemeStyleboxOverride("pressed", CreateBox(SurfacePressed, BorderStrong, 2, 10));
        button.AddThemeStyleboxOverride("disabled", CreateBox(new Color(0.08f, 0.08f, 0.08f, 0.6f), border, 1, 10));
        button.AddThemeColorOverride("font_color", unlocked ? Title : Muted);
        button.AddThemeColorOverride("font_hover_color", Title);
        button.AddThemeColorOverride("font_pressed_color", Title);
        button.AddThemeColorOverride("font_disabled_color", Muted);
    }

    public static Label CreateTextLabel(
        string text,
        int fontSize,
        Color color,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        bool wrap = false)
    {
        Label label = new()
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    public static PanelContainer CreateGlyphIcon(string glyph, Color accentColor, float size = 70f)
    {
        PanelContainer frame = new()
        {
            CustomMinimumSize = new Vector2(size, size),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        frame.AddThemeStyleboxOverride(
            "panel",
            CreateBox(
                new Color(accentColor.R, accentColor.G, accentColor.B, 0.16f),
                accentColor,
                borderWidth: 1,
                radius: 12));

        CenterContainer center = new()
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        Label label = CreateTextLabel(glyph, 20, accentColor, HorizontalAlignment.Center);
        center.AddChild(label);
        frame.AddChild(center);
        return frame;
    }

    public static PanelContainer CreateTextureIcon(Texture2D? texture, float size = 72f)
    {
        PanelContainer frame = new()
        {
            CustomMinimumSize = new Vector2(size, size),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        frame.AddThemeStyleboxOverride(
            "panel",
            CreateBox(
                new Color(Accent.R, Accent.G, Accent.B, 0.08f),
                Border,
                borderWidth: 1,
                radius: 12));

        CenterContainer center = new()
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        TextureRect textureRect = new()
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(size - 18f, size - 18f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        center.AddChild(textureRect);
        frame.AddChild(center);
        return frame;
    }

    public static string GetEditionGlyph(BalatroCardEdition edition)
    {
        return edition switch
        {
            BalatroCardEdition.Foil => "FL",
            BalatroCardEdition.Holographic => "HO",
            BalatroCardEdition.Polychrome => "PC",
            BalatroCardEdition.Negative => "NG",
            _ => "--"
        };
    }

    public static Color GetEditionAccent(BalatroCardEdition edition)
    {
        return edition switch
        {
            BalatroCardEdition.Foil => new Color(0.82f, 0.86f, 0.9f),
            BalatroCardEdition.Holographic => new Color(0.56f, 0.69f, 0.95f),
            BalatroCardEdition.Polychrome => new Color(0.92f, 0.63f, 0.48f),
            BalatroCardEdition.Negative => new Color(0.78f, 0.59f, 0.94f),
            _ => Accent
        };
    }

    private static StyleBoxFlat CreateBox(
        Color background,
        Color borderColor,
        int borderWidth,
        int radius,
        Color? shadowColor = null,
        int shadowSize = 0)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = borderColor,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ShadowColor = shadowColor ?? Colors.Transparent,
            ShadowSize = shadowSize
        };
    }
}
