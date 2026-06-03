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
            OffsetLeft = -360f,
            OffsetRight = 360f,
            OffsetTop = -260f,
            OffsetBottom = 260f
        };
        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.97f),
            BorderColor = new Color(0.94f, 0.82f, 0.58f, 0.96f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12
        };
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        VBoxContainer root = new();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 20f;
        root.OffsetTop = 18f;
        root.OffsetRight = -20f;
        root.OffsetBottom = -18f;
        root.AddThemeConstantOverride("separation", 12);
        panel.AddChild(root);

        Label title = CreateLabel("YUWANCARD-BALATRO_JOKER_BAG.title", 26, new Color(1f, 0.89f, 0.66f));
        root.AddChild(title);

        Label hint = CreateLabel("YUWANCARD-BALATRO_JOKER_BAG.hint", 15, new Color(0.92f, 0.92f, 0.92f));
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(hint);

        HBoxContainer slotRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        slotRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(slotRow);

        for (int i = 0; i < 6; i++)
        {
            int slotIndex = i;
            Button slotButton = new()
            {
                CustomMinimumSize = new Vector2(104f, 72f),
                FocusMode = FocusModeEnum.None
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

        _selectedLabel = CreateTextLabel(string.Empty, 16, new Color(0.95f, 0.84f, 0.42f));
        root.AddChild(_selectedLabel);

        HBoxContainer actionRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.End
        };
        root.AddChild(actionRow);

        _unequipButton = new Button
        {
            Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.unequip").GetFormattedText(),
            CustomMinimumSize = new Vector2(180f, 40f),
            FocusMode = FocusModeEnum.None
        };
        _unequipButton.Pressed += OnUnequipPressed;
        actionRow.AddChild(_unequipButton);

        Button closeButton = new()
        {
            Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.close").GetFormattedText(),
            CustomMinimumSize = new Vector2(120f, 40f),
            FocusMode = FocusModeEnum.None
        };
        closeButton.Pressed += Close;
        actionRow.AddChild(closeButton);

        ScrollContainer scroll = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(scroll);

        _bagGrid = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _bagGrid.AddThemeConstantOverride("h_separation", 10);
        _bagGrid.AddThemeConstantOverride("v_separation", 10);
        scroll.AddChild(_bagGrid);

        _emptyLabel = CreateLabel("YUWANCARD-BALATRO_JOKER_BAG.empty", 16, new Color(0.85f, 0.85f, 0.85f));
        root.AddChild(_emptyLabel);
    }

    private void RefreshUi()
    {
        IReadOnlyList<string> slots = _modifier.GetAllJokerSlotIds();
        for (int i = 0; i < _slotButtons.Count; i++)
        {
            Button button = _slotButtons[i];
            if (!_modifier.IsJokerSlotUnlocked(i))
            {
                button.Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.locked").GetFormattedText();
                button.Disabled = true;
                button.Modulate = Colors.White;
                continue;
            }

            string jokerId = slots[i];
            string title = string.IsNullOrWhiteSpace(jokerId)
                ? new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.empty_slot").GetFormattedText()
                : _modifier.GetJokerTitle(jokerId);
            button.Text = $"{i + 1}\n{title}";
            button.Disabled = false;
            button.TooltipText = title;
            button.Modulate = i == _selectedSlot
                ? new Color(1f, 0.92f, 0.64f)
                : Colors.White;
        }

        if (_selectedLabel != null)
        {
            _selectedLabel.Text = string.Format(
                new LocString("gameplay_ui", "YUWANCARD-BALATRO_JOKER_BAG.selected_slot").GetFormattedText(),
                _selectedSlot + 1);
        }

        if (_unequipButton != null)
        {
            _unequipButton.Disabled = !_modifier.IsJokerSlotUnlocked(_selectedSlot)
                || string.IsNullOrWhiteSpace(slots[_selectedSlot]);
        }

        if (_bagGrid != null)
        {
            foreach (Node child in _bagGrid.GetChildren())
            {
                child.QueueFree();
            }

            IReadOnlyList<string> bag = _modifier.GetJokerBagIds();
            foreach (string jokerId in bag)
            {
                string title = _modifier.GetJokerTitle(jokerId);
                Button button = new()
                {
                    Text = title,
                    TooltipText = title,
                    CustomMinimumSize = new Vector2(200f, 56f),
                    FocusMode = FocusModeEnum.None
                };
                button.Pressed += () => OnBagJokerPressed(jokerId);
                _bagGrid.AddChild(button);
            }

            if (_emptyLabel != null)
            {
                _emptyLabel.Visible = bag.Count == 0;
            }
        }
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

    private static Label CreateLabel(string locKey, int fontSize, Color color)
    {
        return CreateTextLabel(new LocString("gameplay_ui", locKey).GetFormattedText(), fontSize, color);
    }

    private static Label CreateTextLabel(string text, int fontSize, Color color)
    {
        Label label = new()
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private void Close()
    {
        NModalContainer.Instance?.Clear();
    }
}
