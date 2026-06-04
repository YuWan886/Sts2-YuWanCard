using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NJokerBagPopup : Control, IScreenContext
{
    private readonly List<Button> _slotButtons = [];

    private BalatroModifier _modifier = null!;
    private int _selectedSlot;
    private Label? _selectedLabel;
    private GridContainer? _bagGrid;
    private Label? _emptyLabel;
    private Button? _unequipButton;

    public Control? DefaultFocusedControl => _slotButtons.FirstOrDefault(button => !button.Disabled);

    public static void Open(BalatroModifier modifier, int preferredSlot)
    {
        NJokerBagPopup popup = new()
        {
            _modifier = modifier,
            _selectedSlot = Math.Clamp(preferredSlot, 0, Math.Max(0, modifier.GetCurrentJokerCapacity() - 1))
        };
        popup.SetAnchorsPreset(LayoutPreset.FullRect);
        popup.MouseFilter = MouseFilterEnum.Stop;
        NModalContainer.Instance?.Add(popup, showBackstop: true);
    }

    public override void _Ready()
    {
        BuildUi();
        RefreshUi();
    }

    private void BuildUi()
    {
        PanelContainer panel = new()
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -430f,
            OffsetRight = 430f,
            OffsetTop = -310f,
            OffsetBottom = 310f
        };
        panel.AddThemeStyleboxOverride("panel", BalatroUiTheme.CreatePanelStyle());
        AddChild(panel);

        MarginContainer margin = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        panel.AddChild(margin);

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 14);
        margin.AddChild(root);

        root.AddChild(CreateLocLabel("YUWANCARD-BALATRO_JOKER_BAG.title", 28, BalatroUiTheme.Title));

        Label hint = CreateLocLabel("YUWANCARD-BALATRO_JOKER_BAG.hint", 15, BalatroUiTheme.Body);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(hint);

        HBoxContainer slotRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        slotRow.AddThemeConstantOverride("separation", 10);
        root.AddChild(slotRow);

        for (int i = 0; i < 6; i++)
        {
            int slotIndex = i;
            Button slotButton = new()
            {
                CustomMinimumSize = new Vector2(118f, 76f),
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Stop
            };
            slotButton.AddThemeFontSizeOverride("font_size", 13);
            slotButton.Pressed += () =>
            {
                if (_modifier.IsJokerSlotUnlocked(slotIndex))
                {
                    _selectedSlot = slotIndex;
                    RefreshUi();
                }
            };
            _slotButtons.Add(slotButton);
            slotRow.AddChild(slotButton);
        }

        HBoxContainer actionRow = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.End
        };
        actionRow.AddThemeConstantOverride("separation", 10);
        root.AddChild(actionRow);

        _selectedLabel = BalatroUiTheme.CreateTextLabel(string.Empty, 15, BalatroUiTheme.Price);
        _selectedLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actionRow.AddChild(_selectedLabel);

        _unequipButton = new Button
        {
            Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.unequip").GetFormattedText(),
            CustomMinimumSize = new Vector2(180f, 40f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        BalatroUiTheme.ApplyActionButtonStyle(_unequipButton);
        _unequipButton.Pressed += OnUnequipPressed;
        actionRow.AddChild(_unequipButton);

        Button closeButton = new()
        {
            Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.close").GetFormattedText(),
            CustomMinimumSize = new Vector2(132f, 40f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        BalatroUiTheme.ApplyActionButtonStyle(closeButton, primary: true);
        closeButton.Pressed += Close;
        actionRow.AddChild(closeButton);

        ScrollContainer scroll = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddChild(scroll);

        VBoxContainer bagContent = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        bagContent.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(bagContent);

        _emptyLabel = CreateLocLabel("YUWANCARD-BALATRO_JOKER_BAG.empty", 16, BalatroUiTheme.Muted);
        _emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        bagContent.AddChild(_emptyLabel);

        _bagGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _bagGrid.AddThemeConstantOverride("h_separation", 14);
        _bagGrid.AddThemeConstantOverride("v_separation", 14);
        bagContent.AddChild(_bagGrid);
    }

    private void RefreshUi()
    {
        IReadOnlyList<string> slots = _modifier.GetAllJokerSlotIds();
        for (int i = 0; i < _slotButtons.Count; i++)
        {
            Button button = _slotButtons[i];
            bool unlocked = _modifier.IsJokerSlotUnlocked(i);
            BalatroUiTheme.ApplySlotButtonStyle(button, selected: i == _selectedSlot, unlocked: unlocked);

            if (!unlocked)
            {
                button.Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.locked").GetFormattedText();
                button.Disabled = true;
                button.TooltipText = string.Empty;
                continue;
            }

            string jokerId = slots[i];
            string title = string.IsNullOrWhiteSpace(jokerId)
                ? new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.empty_slot").GetFormattedText()
                : _modifier.GetJokerTitle(jokerId);
            button.Text = $"{i + 1}\n{title}";
            button.Disabled = false;
            button.TooltipText = title;
        }

        if (_selectedLabel != null)
        {
            string selectedText = string.Format(
                new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.selected_slot").GetRawText(),
                _selectedSlot + 1);
            string currentTitle = string.IsNullOrWhiteSpace(slots[_selectedSlot])
                ? new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.empty_slot").GetFormattedText()
                : _modifier.GetJokerTitle(slots[_selectedSlot]);
            _selectedLabel.Text = $"{selectedText}  {currentTitle}";
        }

        if (_unequipButton != null)
        {
            _unequipButton.Disabled = !_modifier.IsJokerSlotUnlocked(_selectedSlot)
                || string.IsNullOrWhiteSpace(slots[_selectedSlot]);
        }

        if (_bagGrid == null)
        {
            return;
        }

        foreach (Node child in _bagGrid.GetChildren())
        {
            child.QueueFree();
        }

        IReadOnlyList<string> bag = _modifier.GetJokerBagIds();
        foreach (string jokerId in bag)
        {
            _bagGrid.AddChild(CreateBagJokerButton(jokerId));
        }

        if (_emptyLabel != null)
        {
            _emptyLabel.Visible = bag.Count == 0;
        }

        _bagGrid.Visible = bag.Count > 0;
    }

    private Button CreateBagJokerButton(string jokerId)
    {
        string title = _modifier.GetJokerTitle(jokerId);
        string description = _modifier.GetJokerDescription(jokerId);
        Texture2D? icon = _modifier.GetJokerIcon(jokerId);

        Button button = new()
        {
            CustomMinimumSize = new Vector2(378f, 208f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            TooltipText = $"{title}\n{description}"
        };
        BalatroUiTheme.ApplyCardButtonStyle(button);
        button.Pressed += () => OnBagJokerPressed(jokerId);

        MarginContainer margin = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        button.AddChild(margin);

        VBoxContainer layout = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        layout.AddThemeConstantOverride("separation", 10);
        margin.AddChild(layout);

        Control iconNode = icon != null
            ? BalatroUiTheme.CreateTextureIcon(icon, 72f)
            : BalatroUiTheme.CreateGlyphIcon("JK", BalatroUiTheme.Accent, 72f);
        layout.AddChild(iconNode);

        layout.AddChild(BalatroUiTheme.CreateTextLabel(title, 18, BalatroUiTheme.Title, HorizontalAlignment.Center));

        Label descriptionLabel = BalatroUiTheme.CreateTextLabel(description, 14, BalatroUiTheme.Body, HorizontalAlignment.Center, wrap: true);
        descriptionLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(descriptionLabel);

        Control spacer = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddChild(spacer);

        string equipText = string.Format(
            new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.selected_slot").GetRawText(),
            _selectedSlot + 1);
        layout.AddChild(BalatroUiTheme.CreateTextLabel(equipText, 14, BalatroUiTheme.Price, HorizontalAlignment.Center));

        return button;
    }

    private static Label CreateLocLabel(string locKey, int fontSize, Color color)
    {
        return BalatroUiTheme.CreateTextLabel(new LocString("gameplay_ui", locKey).GetFormattedText(), fontSize, color);
    }

    private void OnBagJokerPressed(string jokerId)
    {
        if (_modifier.TryEquipBagJoker(jokerId, _selectedSlot))
        {
            RefreshUi();
        }
    }

    private void OnUnequipPressed()
    {
        if (_modifier.TryUnequipJoker(_selectedSlot))
        {
            RefreshUi();
        }
    }

    private void Close()
    {
        NModalContainer.Instance?.Clear();
    }
}
