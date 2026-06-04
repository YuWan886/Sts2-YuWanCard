using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NJokerSlotBar : Control
{
    private const float SlotWidth = 56f;
    private const float SlotHeight = 56f;
    private const float BagWidth = 70f;
    private const float RowGap = 6f;

    private readonly List<Button> _slotButtons = [];

    private HBoxContainer? _row;
    private Button? _bagButton;

    public override void _Ready()
    {
        Name = "YuWanBalatroJokerBar";
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 6);
        AddChild(root);

        _row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        _row.AddThemeConstantOverride("separation", (int)RowGap);
        root.AddChild(_row);

        for (int i = 0; i < 6; i++)
        {
            int slotIndex = i;
            Button slotButton = new()
            {
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Stop
            };
            slotButton.AddThemeFontSizeOverride("font_size", 11);
            BalatroUiTheme.ApplySlotButtonStyle(slotButton, selected: false, unlocked: true);
            slotButton.Pressed += () => OpenBag(slotIndex);
            _slotButtons.Add(slotButton);
            _row.AddChild(slotButton);
        }

        _bagButton = new Button
        {
            Text = Loc("YUWANCARD-BALATRO_JOKER_BAR.bag_button"),
            CustomMinimumSize = new Vector2(BagWidth, SlotHeight),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        _bagButton.AddThemeFontSizeOverride("font_size", 12);
        BalatroUiTheme.ApplyActionButtonStyle(_bagButton);
        _bagButton.Pressed += () => OpenBag(0);
        _row.AddChild(_bagButton);
    }

    public override void _Process(double delta)
    {
        if (RunManager.Instance?.State is not RunState state)
        {
            Visible = false;
            return;
        }

        BalatroModifier? modifier = BalatroModifier.GetInstance(state);
        Visible = modifier != null;
        if (modifier == null)
        {
            return;
        }

        IReadOnlyList<string> slots = modifier.GetAllJokerSlotIds();
        int unlockedCount = 0;
        for (int i = 0; i < _slotButtons.Count; i++)
        {
            Button button = _slotButtons[i];
            if (!modifier.IsJokerSlotUnlocked(i))
            {
                button.Visible = false;
                button.Disabled = true;
                button.TooltipText = Loc("YUWANCARD-BALATRO_JOKER_BAR.locked_tooltip");
                continue;
            }

            unlockedCount++;
            button.Visible = true;
            BalatroUiTheme.ApplySlotButtonStyle(button, selected: false, unlocked: true);
            string jokerId = slots[i];
            string title = string.IsNullOrWhiteSpace(jokerId)
                ? Loc("YUWANCARD-BALATRO_JOKER_BAR.empty_slot")
                : modifier.GetJokerTitle(jokerId);
            button.Text = $"{i + 1}\n{ShortenTitle(title)}";
            button.TooltipText = title;
            button.Disabled = false;
        }

        if (_bagButton != null)
        {
            _bagButton.TooltipText = string.Format(
                LocRaw("YUWANCARD-BALATRO_JOKER_BAR.bag_tooltip"),
                modifier.GetJokerBagIds().Count);
        }

        CustomMinimumSize = new Vector2(GetPreferredHudWidth(unlockedCount), SlotHeight);
    }

    private static string ShortenTitle(string title)
    {
        return title.Length <= 8 ? title : title[..8];
    }

    private void OpenBag(int preferredSlot)
    {
        if (RunManager.Instance?.State is not RunState state)
        {
            return;
        }

        BalatroModifier? modifier = BalatroModifier.GetInstance(state);
        if (modifier == null)
        {
            return;
        }

        int targetSlot = modifier.IsJokerSlotUnlocked(preferredSlot) ? preferredSlot : 0;
        NJokerBagPopup.Open(modifier, targetSlot);
    }

    private static string Loc(string key)
    {
        return new LocString("gameplay_ui", key).GetFormattedText();
    }

    private static string LocRaw(string key)
    {
        return new LocString("gameplay_ui", key).GetRawText();
    }

    public float GetPreferredHudWidth()
    {
        int unlockedCount = RunManager.Instance?.State is RunState state && BalatroModifier.GetInstance(state) is { } modifier
            ? modifier.GetCurrentJokerCapacity()
            : 3;
        return GetPreferredHudWidth(unlockedCount);
    }

    private static float GetPreferredHudWidth(int unlockedCount)
    {
        int elementCount = Math.Max(1, unlockedCount) + 1;
        float gapWidth = Math.Max(0, elementCount - 1) * RowGap;
        return unlockedCount * SlotWidth + BagWidth + gapWidth;
    }
}
