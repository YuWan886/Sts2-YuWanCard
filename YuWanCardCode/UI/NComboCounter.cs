using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NComboCounter : Control
{
    private const float HudWidth = 296f;
    private const float HudHeight = 150f;
    private const float TopMargin = 88f;
    private const float RightMargin = 34f;

    private readonly RandomNumberGenerator _rng = new();

    private Control? _motionRoot;
    private Control? _layoutRoot;
    private Control? _fxLayer;
    private Control? _multRoot;
    private Control? _bonusRoot;
    private Control? _comboRoot;
    private Label? _multShadowLabel;
    private Label? _multLabel;
    private Label? _bonusShadowLabel;
    private Label? _bonusLabel;
    private Label? _comboShadowLabel;
    private Label? _comboLabel;
    private float _lastCombo = -1f;
    private float _lastMultiplier = -1f;
    private float _impact;
    private float _motionJolt;
    private double _elapsed;

    public bool HudEnabled { get; set; } = true;

    public override void _Ready()
    {
        Name = "YuWanBalatroComboCounter";
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(HudWidth, HudHeight);
        CustomMinimumSize = Size;
        ZIndex = 90;
        _rng.Randomize();

        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);

        MarginContainer margin = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        _motionRoot = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _motionRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddChild(_motionRoot);

        _layoutRoot = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _layoutRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _motionRoot.AddChild(_layoutRoot);

        _multRoot = CreateShadowedLabel(_layoutRoot, out _multShadowLabel, out _multLabel, 58, new Vector2(0f, 12f), new Vector2(178f, 62f));
        _bonusRoot = CreateShadowedLabel(_layoutRoot, out _bonusShadowLabel, out _bonusLabel, 24, new Vector2(188f, 24f), new Vector2(70f, 28f));
        _comboRoot = CreateShadowedLabel(_layoutRoot, out _comboShadowLabel, out _comboLabel, 24, new Vector2(2f, 82f), new Vector2(180f, 32f));

        _fxLayer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _fxLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _motionRoot.AddChild(_fxLayer);

        CallDeferred(nameof(UpdateAnchorPosition));
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;

        if (RunManager.Instance?.State is not RunState state)
        {
            Visible = false;
            return;
        }

        BalatroModifier? modifier = BalatroModifier.GetInstance(state);
        Player? player = LocalContext.GetMe(state.Players) ?? state.Players.FirstOrDefault();
        bool inCombat = state.CurrentRoom is CombatRoom;
        Visible = HudEnabled && modifier != null && player != null && inCombat;
        if (!Visible || modifier == null ||
            _multLabel == null || _bonusLabel == null || _comboLabel == null ||
            _multShadowLabel == null || _bonusShadowLabel == null || _comboShadowLabel == null)
        {
            return;
        }

        float combo = modifier.GetComboCounter(player);
        float multiplier = modifier.GetComboMultiplier(player);
        float comboBonus = combo * 0.1f;
        float nonComboBonus = Mathf.Max(0f, multiplier - 1f - comboBonus);

        UpdateAnchorPosition();
        UpdatePalette(combo, multiplier);
        UpdateText(multiplier, combo, comboBonus, nonComboBonus);
        UpdateIdleMotion(delta, combo);

        if (_lastCombo >= 0f && !Mathf.IsEqualApprox(_lastCombo, combo))
        {
            if (combo > _lastCombo)
            {
                float increase = combo - _lastCombo;
                PlayIncreaseFeedback(combo, increase);
            }
            else
            {
                PlayDropFeedback();
            }
        }

        if (_lastMultiplier >= 0f && !Mathf.IsEqualApprox(_lastMultiplier, multiplier))
        {
            float rise = Mathf.Max(0f, multiplier - _lastMultiplier);
            if (rise > 0f)
            {
                PlayMultiplierFeedback(combo, rise);
            }
        }

        _lastCombo = combo;
        _lastMultiplier = multiplier;
    }

    private static Control CreateShadowedLabel(
        Control parent,
        out Label shadow,
        out Label main,
        int fontSize,
        Vector2 position,
        Vector2 size)
    {
        Control root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Position = position,
            Size = size,
            CustomMinimumSize = size,
            PivotOffset = size * 0.5f
        };
        parent.AddChild(root);

        shadow = CreateLabel(fontSize, size, new Vector2(4f, 4f), new Color(0f, 0f, 0f, 0.96f));
        root.AddChild(shadow);

        main = CreateLabel(fontSize, size, Vector2.Zero, Colors.White);
        root.AddChild(main);
        return root;
    }

    private static Label CreateLabel(int fontSize, Vector2 size, Vector2 position, Color color)
    {
        Label label = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Position = position,
            Size = size,
            CustomMinimumSize = size,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private void UpdateText(float multiplier, float combo, float comboBonus, float nonComboBonus)
    {
        string multiplierText = multiplier.ToString("0.0");
        string bonusText = $"+{comboBonus:0.0}";
        string comboText = string.Format(LocRaw("YUWANCARD-BALATRO_HUD.combo_compact"), combo);

        ApplyText(_multLabel, _multShadowLabel, multiplierText);
        ApplyText(_bonusLabel, _bonusShadowLabel, bonusText);
        ApplyText(_comboLabel, _comboShadowLabel, comboText);

        TooltipText = nonComboBonus > 0.01f
            ? string.Format(LocRaw("YUWANCARD-BALATRO_HUD.legend_bonus_tooltip"), nonComboBonus)
            : string.Empty;
    }

    private void UpdatePalette(float combo, float multiplier)
    {
        if (_multLabel == null || _bonusLabel == null || _comboLabel == null)
        {
            return;
        }

        Color accent = ResolveAccentColor(combo);
        Color multiplierColor = ResolveMultiplierColor(combo, multiplier);
        Color comboColor = ResolveComboColor(combo);
        Color bonusColor = accent.Lerp(Colors.White, 0.18f);

        ApplyTextColor(_multLabel, multiplierColor);
        ApplyTextColor(_bonusLabel, bonusColor);
        ApplyTextColor(_comboLabel, comboColor);
    }

    private void UpdateIdleMotion(double delta, float combo)
    {
        if (_motionRoot == null)
        {
            return;
        }

        _impact = Mathf.MoveToward(_impact, 0f, (float)delta * 1.7f);
        _motionJolt = Mathf.MoveToward(_motionJolt, 0f, (float)delta * 14f);

        float comboEnergy = Mathf.Clamp(combo / 20f, 0f, 1f);
        float wave = Mathf.Sin((float)_elapsed * (4.4f + comboEnergy * 2.6f));
        float pulse = 1f + comboEnergy * 0.02f + _impact * 0.05f + wave * (0.005f + comboEnergy * 0.008f);

        _motionRoot.Scale = Vector2.One * pulse;
        _motionRoot.Position = new Vector2(_motionJolt, wave * (0.8f + comboEnergy * 1.6f));
    }

    private void PlayIncreaseFeedback(float combo, float increase)
    {
        Color accent = ResolveAccentColor(combo);
        _impact = Mathf.Clamp(_impact + 0.18f + increase * 0.06f, 0f, 1.25f);
        _motionJolt = 5f + Mathf.Min(6f, increase * 1.2f);

        AnimatePop(_multRoot, 1.18f + Mathf.Min(0.08f, increase * 0.02f), 0.09f, 0.18f);
        AnimatePop(_comboRoot, 1.12f + Mathf.Min(0.06f, increase * 0.02f), 0.08f, 0.15f);
        AnimatePop(_bonusRoot, 1.08f + Mathf.Min(0.04f, increase * 0.01f), 0.07f, 0.13f);
        FlashLabels(accent.Lerp(Colors.White, 0.2f), 0.22f);
        SpawnBurst(accent, 10 + Mathf.RoundToInt(increase * 2f), true);
    }

    private void PlayMultiplierFeedback(float combo, float rise)
    {
        Color accent = ResolveAccentColor(combo).Lerp(Colors.White, 0.12f);
        AnimatePop(_multRoot, 1.24f + Mathf.Min(0.08f, rise * 0.08f), 0.08f, 0.2f);
        FlashLabels(accent, 0.16f);
        SpawnBurst(accent, 6 + Mathf.RoundToInt(rise * 4f), true);
    }

    private void PlayDropFeedback()
    {
        Color dropColor = new(0.52f, 0.72f, 1f);
        _impact = Mathf.Max(_impact, 0.1f);
        _motionJolt = -3f;

        AnimatePop(_multRoot, 0.94f, 0.06f, 0.14f);
        AnimatePop(_comboRoot, 0.9f, 0.06f, 0.14f);
        FlashLabels(dropColor, 0.12f);
        SpawnBurst(dropColor, 7, false);
    }

    private static void AnimatePop(Control? node, float peakScale, float upDuration, float downDuration)
    {
        if (node == null)
        {
            return;
        }

        node.Scale = Vector2.One;
        Tween tween = node.CreateTween();
        tween.TweenProperty(node, "scale", Vector2.One * peakScale, upDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(node, "scale", Vector2.One, downDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void FlashLabels(Color color, float alpha)
    {
        if (_multLabel == null || _bonusLabel == null || _comboLabel == null)
        {
            return;
        }

        FlashLabel(_multLabel, color, alpha, 0.22f);
        FlashLabel(_bonusLabel, color, alpha * 0.85f, 0.18f);
        FlashLabel(_comboLabel, color, alpha * 0.9f, 0.2f);
    }

    private static void FlashLabel(Label label, Color color, float alpha, float fadeDuration)
    {
        Color baseColor = label.GetThemeColor("font_color");
        Color flashColor = baseColor.Lerp(color, Mathf.Clamp(alpha, 0f, 1f));
        label.Modulate = Colors.White;

        Tween tween = label.CreateTween();
        tween.TweenProperty(label, "modulate", flashColor, 0.04f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(label, "modulate", Colors.White, fadeDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void SpawnBurst(Color color, int count, bool upward)
    {
        if (_fxLayer == null)
        {
            return;
        }

        int burstCount = Mathf.Clamp(count, 4, 16);
        for (int i = 0; i < burstCount; i++)
        {
            Label particle = new()
            {
                Text = i % 3 == 0 ? "+" : "*",
                MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2(150f + _rng.RandfRange(-18f, 28f), 46f + _rng.RandfRange(-12f, 12f))
            };
            particle.AddThemeFontSizeOverride("font_size", _rng.RandiRange(14, 20));
            particle.AddThemeColorOverride("font_color", color);
            _fxLayer.AddChild(particle);

            Vector2 drift = new(
                _rng.RandfRange(-56f, 56f),
                upward ? _rng.RandfRange(-52f, -18f) : _rng.RandfRange(18f, 44f));

            Tween tween = particle.CreateTween();
            tween.SetParallel();
            tween.TweenProperty(particle, "position", particle.Position + drift, 0.36f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(particle, "modulate:a", 0f, 0.36f);
            tween.TweenProperty(particle, "scale", Vector2.One * _rng.RandfRange(0.7f, 1.4f), 0.36f);
            tween.Finished += particle.QueueFree;
        }
    }

    private void UpdateAnchorPosition()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        Position = new Vector2(
            viewportSize.X - HudWidth - RightMargin,
            TopMargin);
    }

    private static void ApplyText(Label? main, Label? shadow, string text)
    {
        if (main == null || shadow == null)
        {
            return;
        }

        main.Text = text;
        shadow.Text = text;
    }

    private static void ApplyTextColor(Label label, Color color)
    {
        label.AddThemeColorOverride("font_color", color);
    }

    private static Color ResolveAccentColor(float combo)
    {
        if (combo >= 24f)
        {
            float hue = (Time.GetTicksMsec() % 1400) / 1400f;
            return Color.FromHsv(hue, 0.68f, 1f);
        }

        if (combo >= 16f)
        {
            return new Color(1f, 0.38f, 0.2f);
        }

        if (combo >= 8f)
        {
            return new Color(0.99f, 0.78f, 0.2f);
        }

        return new Color(0.85f, 0.91f, 1f);
    }

    private static Color ResolveMultiplierColor(float combo, float multiplier)
    {
        if (combo >= 24f)
        {
            float hue = (Time.GetTicksMsec() % 1100) / 1100f;
            return Color.FromHsv(hue, 0.42f, 1f);
        }

        if (multiplier >= 3f)
        {
            return new Color(1f, 0.9f, 0.78f);
        }

        if (combo >= 10f)
        {
            return new Color(1f, 0.96f, 0.9f);
        }

        return Colors.White;
    }

    private static Color ResolveComboColor(float combo)
    {
        if (combo >= 24f)
        {
            return new Color(1f, 0.76f, 0.92f);
        }

        if (combo >= 16f)
        {
            return new Color(1f, 0.82f, 0.42f);
        }

        if (combo >= 8f)
        {
            return new Color(0.96f, 0.89f, 0.55f);
        }

        return new Color(0.92f, 0.92f, 0.94f);
    }

    private static string LocRaw(string key)
    {
        return new LocString("gameplay_ui", key).GetRawText();
    }
}
