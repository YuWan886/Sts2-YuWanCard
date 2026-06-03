using Godot;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NComboCounter : Control
{
    private readonly RandomNumberGenerator _rng = new();

    private Label? _comboLabel;
    private Label? _multLabel;
    private Control? _particleLayer;
    private float _lastCombo = -1f;
    private float _lastMultiplier = -1f;

    public override void _Ready()
    {
        Name = "YuWanBalatroComboCounter";
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 2);
        AddChild(root);

        _comboLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _comboLabel.AddThemeFontSizeOverride("font_size", 22);
        root.AddChild(_comboLabel);

        _multLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _multLabel.AddThemeFontSizeOverride("font_size", 15);
        _multLabel.AddThemeColorOverride("font_color", new Color(0.94f, 0.94f, 0.94f));
        root.AddChild(_multLabel);

        _particleLayer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _particleLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_particleLayer);
    }

    public override void _Process(double delta)
    {
        if (RunManager.Instance?.State is not RunState state)
        {
            Visible = false;
            return;
        }

        BalatroModifier? modifier = BalatroModifier.GetInstance(state);
        bool inCombat = state.CurrentRoom is CombatRoom;
        Visible = modifier != null && inCombat;
        if (modifier == null || !inCombat || _comboLabel == null || _multLabel == null)
        {
            return;
        }

        float combo = modifier.ComboCounter;
        float multiplier = modifier.ComboMultiplier;
        _comboLabel.Text = $"COMBO {combo:0.#}";
        _multLabel.Text = $"MULT x{multiplier:0.0}";
        _comboLabel.AddThemeColorOverride("font_color", ResolveComboColor(combo));

        if (_lastCombo >= 0f && !Mathf.IsEqualApprox(_lastCombo, combo))
        {
            if (combo > _lastCombo)
            {
                AnimateLabel(_comboLabel, 1.18f);
                SpawnBurst(new Color(0.96f, 0.84f, 0.32f), true);
            }
            else if (_lastCombo > 0f)
            {
                AnimateLabel(_comboLabel, 0.92f);
                SpawnBurst(new Color(0.74f, 0.45f, 1f), false);
            }
        }

        if (_lastMultiplier >= 0f && !Mathf.IsEqualApprox(_lastMultiplier, multiplier))
        {
            AnimateLabel(_multLabel, 1.08f);
        }

        _lastCombo = combo;
        _lastMultiplier = multiplier;
    }

    private static Color ResolveComboColor(float combo)
    {
        if (combo >= 25f)
        {
            float hue = (Time.GetTicksMsec() % 1800) / 1800f;
            return Color.FromHsv(hue, 0.72f, 1f);
        }

        if (combo >= 15f)
        {
            return new Color(1f, 0.63f, 0.24f);
        }

        if (combo >= 5f)
        {
            return new Color(0.98f, 0.86f, 0.36f);
        }

        return Colors.White;
    }

    private static void AnimateLabel(Control control, float peakScale)
    {
        control.Scale = Vector2.One;
        Tween tween = control.CreateTween();
        tween.TweenProperty(control, "scale", Vector2.One * peakScale, 0.08f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(control, "scale", Vector2.One, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void SpawnBurst(Color color, bool upward)
    {
        if (_particleLayer == null)
        {
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            Label particle = new()
            {
                Text = "*",
                MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2(110f + _rng.RandfRange(-16f, 16f), 12f + _rng.RandfRange(-6f, 6f))
            };
            particle.AddThemeFontSizeOverride("font_size", 14);
            particle.AddThemeColorOverride("font_color", color);
            _particleLayer.AddChild(particle);

            Vector2 drift = new(_rng.RandfRange(-28f, 28f), upward ? _rng.RandfRange(-36f, -12f) : _rng.RandfRange(12f, 34f));
            Tween tween = particle.CreateTween();
            tween.SetParallel();
            tween.TweenProperty(particle, "position", particle.Position + drift, 0.35f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(particle, "modulate:a", 0f, 0.35f);
            tween.TweenProperty(particle, "scale", Vector2.One * _rng.RandfRange(0.6f, 1.4f), 0.35f);
            tween.Finished += particle.QueueFree;
        }
    }
}
