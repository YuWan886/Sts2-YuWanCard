using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NJokerSlotBar : Control
{
    private readonly List<Button> _slotButtons = [];

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

        Label title = new()
        {
            Text = Loc("YUWANCARD-BALATRO_JOKER_BAR.title"),
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.84f, 0.64f));
        root.AddChild(title);

        HBoxContainer row = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 6);
        root.AddChild(row);

        for (int i = 0; i < 6; i++)
        {
            int slotIndex = i;
            Button slotButton = new()
            {
                CustomMinimumSize = new Vector2(56f, 56f),
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Stop
            };
            slotButton.AddThemeFontSizeOverride("font_size", 11);
            slotButton.Pressed += () => OpenBag(slotIndex);
            _slotButtons.Add(slotButton);
            row.AddChild(slotButton);
        }

        _bagButton = new Button
        {
            Text = Loc("YUWANCARD-BALATRO_JOKER_BAR.bag_button"),
            CustomMinimumSize = new Vector2(70f, 56f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        _bagButton.AddThemeFontSizeOverride("font_size", 12);
        _bagButton.Pressed += () => OpenBag(0);
        row.AddChild(_bagButton);
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
        for (int i = 0; i < _slotButtons.Count; i++)
        {
            Button button = _slotButtons[i];
            if (!modifier.IsJokerSlotUnlocked(i))
            {
                button.Text = Loc("YUWANCARD-BALATRO_JOKER_BAR.locked_short");
                button.Disabled = true;
                button.TooltipText = Loc("YUWANCARD-BALATRO_JOKER_BAR.locked_tooltip");
                continue;
            }

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
}
