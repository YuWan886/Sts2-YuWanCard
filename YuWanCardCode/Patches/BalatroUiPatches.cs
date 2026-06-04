using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;
using YuWanCard.Modifiers;
using YuWanCard.UI;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NTopBar))]
public static class BalatroTopBarUiPatch
{
    private const string ToggleButtonName = "BalatroUiToggleButton";
    private const string PanelName = "YuWanBalatroHudPanel";
    private const string CounterName = "YuWanBalatroComboCounter";
    private const string ButtonIconPath = "res://YuWanCard/images/modifiers/balatro.png";

    private static Button? _toggleButton;
    private static NBalatroHudPanel? _panel;
    private static NComboCounter? _comboCounter;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void AddBalatroUi(NTopBar __instance)
    {
        EnsureToggleButton(__instance);
        EnsureHudPanel(__instance);
        EnsureComboCounter(__instance);
        RefreshVisibility(RunManager.Instance?.State);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NTopBar.Initialize))]
    public static void RefreshVisibility(IRunState runState)
    {
        RefreshVisibility(runState as RunState);
    }

    private static void EnsureToggleButton(NTopBar topBar)
    {
        Node? mapButton = topBar.Map;
        Node? parent = mapButton?.GetParent();
        if (mapButton == null || parent == null)
        {
            return;
        }

        if (parent.GetNodeOrNull<Button>(ToggleButtonName) is { } existingButton)
        {
            _toggleButton = existingButton;
            return;
        }

        Button button = CreateToggleButton();
        parent.AddChild(button);
        parent.MoveChild(button, mapButton.GetIndex());
        _toggleButton = button;
    }

    private static void EnsureHudPanel(NTopBar topBar)
    {
        if (_panel != null && GodotObject.IsInstanceValid(_panel))
        {
            return;
        }

        Node? parent = topBar.GetParent();
        if (parent == null)
        {
            return;
        }

        if (parent.GetNodeOrNull<NBalatroHudPanel>(PanelName) is { } existingPanel)
        {
            _panel = existingPanel;
            return;
        }

        NBalatroHudPanel panel = new();
        panel.Name = PanelName;
        parent.CallDeferred(Node.MethodName.AddChild, panel);
        _panel = panel;
    }

    private static void EnsureComboCounter(NTopBar topBar)
    {
        if (_comboCounter != null && GodotObject.IsInstanceValid(_comboCounter))
        {
            return;
        }

        Node? parent = topBar.GetParent();
        if (parent == null)
        {
            return;
        }

        if (parent.GetNodeOrNull<NComboCounter>(CounterName) is { } existingCounter)
        {
            _comboCounter = existingCounter;
            return;
        }

        NComboCounter counter = new()
        {
            Name = CounterName
        };
        parent.CallDeferred(Node.MethodName.AddChild, counter);
        _comboCounter = counter;
    }

    private static Button CreateToggleButton()
    {
        Button button = new()
        {
            Name = ToggleButtonName,
            TooltipText = new LocString("gameplay_ui", "YUWANCARD-BALATRO_HUD.toggle_tooltip").GetFormattedText(),
            CustomMinimumSize = new Vector2(80f, 80f),
            FocusMode = Control.FocusModeEnum.None
        };

        StyleBoxFlat styleBox = new()
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderColor = new Color(0f, 0f, 0f, 0f)
        };
        button.AddThemeStyleboxOverride("normal", styleBox);
        button.AddThemeStyleboxOverride("hover", styleBox);
        button.AddThemeStyleboxOverride("pressed", styleBox);
        button.AddThemeStyleboxOverride("focus", styleBox);

        Texture2D? icon = GD.Load<Texture2D>(ButtonIconPath);
        if (icon != null)
        {
            TextureRect iconRect = new()
            {
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                CustomMinimumSize = new Vector2(60f, 60f),
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -30f,
                OffsetTop = -30f,
                OffsetRight = 30f,
                OffsetBottom = 30f,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            button.AddChild(iconRect);
        }

        button.Pressed += static () => NBalatroHudPanel.ToggleOpen();
        return button;
    }

    private static void RefreshVisibility(RunState? state)
    {
        bool isActive = state != null && BalatroModifier.GetInstance(state) != null;

        if (_toggleButton != null && GodotObject.IsInstanceValid(_toggleButton))
        {
            _toggleButton.Visible = isActive;
        }

        if (_panel != null && GodotObject.IsInstanceValid(_panel))
        {
            _panel.Visible = isActive && NBalatroHudPanel.IsOpen;
        }

        if (_comboCounter != null && GodotObject.IsInstanceValid(_comboCounter))
        {
            _comboCounter.HudEnabled = isActive && NBalatroHudPanel.IsOpen;
            _comboCounter.Visible = _comboCounter.HudEnabled;
        }
    }
}

[HarmonyPatch(typeof(NTopBarModifier), nameof(NTopBarModifier._Ready))]
public static class BalatroTopBarModifierIconPatch
{
    [HarmonyPostfix]
    public static void Postfix(NTopBarModifier __instance)
    {
        TextureRect? icon = __instance.GetNodeOrNull<TextureRect>("Icon");
        if (icon == null)
        {
            return;
        }

        ModifierModel? modifier = AccessTools.Field(typeof(NTopBarModifier), "_modifier")?.GetValue(__instance) as ModifierModel;
        if (modifier is not BalatroModifier)
        {
            return;
        }

        icon.Texture = modifier.Icon;
    }
}
