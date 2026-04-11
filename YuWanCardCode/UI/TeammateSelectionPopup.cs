using System.ComponentModel;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.UI;

public partial class TeammateSelectionPopup : Control, IScreenContext
{
    private MerchantEntry _entry = null!;
    private int _cost;
    private Dictionary<ulong, int> _goldData = new();
    private TaskCompletionSource<Player?> _tcs = new();
    private List<Button> _teammateButtons = new();
    
    public Control? DefaultFocusedControl => _teammateButtons.FirstOrDefault(b => !b.Disabled);
    
    public static TeammateSelectionPopup Create(MerchantEntry entry, int cost, Dictionary<ulong, int> goldData)
    {
        var popup = new TeammateSelectionPopup();
        popup._entry = entry;
        popup._cost = cost;
        popup._goldData = goldData;
        popup.SetAnchorsPreset(LayoutPreset.FullRect);
        popup.MouseFilter = MouseFilterEnum.Stop;
        
        return popup;
    }
    
    public async Task<Player?> WaitForSelection()
    {
        return await _tcs.Task;
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void _Ready()
    {
        base._Ready();
        SetupContent();
    }
    
    private void SetupContent()
    {
        var panel = new PanelContainer();
        panel.AnchorLeft = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorTop = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.OffsetLeft = -300;
        panel.OffsetRight = 300f;
        panel.OffsetTop = -300f;
        panel.OffsetBottom = 300f;
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        styleBox.BorderColor = new Color(0.4f, 0.35f, 0.25f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(15);
        panel.AddThemeStyleboxOverride("panel", styleBox);
        
        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(vbox);
        
        var titleLabel = CreateLabel(new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.select_teammate").GetRawText(), true);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(titleLabel);
        
        vbox.AddChild(new HSeparator());
        
        string entryName = GetEntryName(_entry);
        var entryLabel = CreateLabel($"{entryName} - {_cost}G", false);
        entryLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(entryLabel);
        
        vbox.AddChild(new HSeparator());
        
        var scrollContainer = new ScrollContainer();
        scrollContainer.CustomMinimumSize = new Vector2(600, 200);
        vbox.AddChild(scrollContainer);
        
        var buttonContainer = new VBoxContainer();
        scrollContainer.AddChild(buttonContainer);
        
        var runState = RunManager.Instance?.State;
        var localPlayer = LocalContext.GetMe(runState);
        
        if (runState != null && localPlayer != null)
        {
            foreach (var teammate in runState.Players)
            {
                if (teammate.NetId == localPlayer.NetId) continue;
                
                int teammateGold = _goldData.TryGetValue(teammate.NetId, out var gold) ? gold : teammate.Gold;
                string playerName = GetPlayerName(teammate);
                
                var btn = new Button();
                btn.Text = $"{playerName} ({teammateGold}G)";
                btn.AddThemeFontSizeOverride("font_size", 36);
                
                if (teammateGold >= _cost)
                {
                    ulong netId = teammate.NetId;
                    btn.Pressed += () => SelectTeammate(netId);
                }
                else
                {
                    btn.Disabled = true;
                    string notEnough = new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.not_enough_gold").GetRawText();
                    btn.Text += $" - {notEnough}";
                }
                
                _teammateButtons.Add(btn);
                buttonContainer.AddChild(btn);
            }
        }
        
        vbox.AddChild(new HSeparator());
        
        var cancelButton = new Button();
        cancelButton.Text = new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.cancel").GetRawText();
        cancelButton.AddThemeFontSizeOverride("font_size", 36);
        cancelButton.Pressed += Cancel;
        vbox.AddChild(cancelButton);
        
        AddChild(panel);
        
        Modulate = Colors.Transparent;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1f, 0.2);
    }
    
    private Label CreateLabel(string text, bool isTitle)
    {
        var label = new Label();
        label.Text = text;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        
        if (isTitle)
        {
            label.AddThemeFontSizeOverride("font_size", 48);
            label.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.5f));
        }
        else
        {
            label.AddThemeFontSizeOverride("font_size", 36);
        }
        
        return label;
    }
    
    private string GetPlayerName(Player player)
    {
        return PlatformUtil.GetPlayerName(RunManager.Instance?.NetService?.Platform ?? PlatformType.None, player.NetId);
    }
    
    private string GetEntryName(MerchantEntry entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry => cardEntry.CreationResult?.Card?.Title ?? "Card",
            MerchantRelicEntry relicEntry => relicEntry.Model?.Title?.GetFormattedText() ?? "Relic",
            MerchantPotionEntry potionEntry => potionEntry.Model?.Title?.GetFormattedText() ?? "Potion",
            MerchantCardRemovalEntry => new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.card_removal").GetRawText(),
            _ => "Item"
        };
    }
    
    private void SelectTeammate(ulong netId)
    {
        var runState = RunManager.Instance?.State;
        if (runState == null) return;
        
        Player? selectedPlayer = runState.Players.FirstOrDefault(p => p.NetId == netId);
        if (selectedPlayer != null)
        {
            _tcs.TrySetResult(selectedPlayer);
            Close();
        }
    }
    
    private void Cancel()
    {
        _tcs.TrySetResult(null);
        Close();
    }
    
    private void Close()
    {
        NModalContainer.Instance?.Clear();
    }
}
