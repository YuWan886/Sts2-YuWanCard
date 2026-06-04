using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Balatro;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NBalatroMerchantExtension : Control
{
    private const float TabRowTop = 92f;
    private const float TabRowHeight = 42f;
    private const float StationRootTop = 148f;
    private const float StationSideMargin = 92f;
    private const float StationBottomMargin = 40f;
    private const float OfferCardWidth = 312f;
    private const float OfferCardHeight = 272f;
    private const string DefaultOfferIconPath = "res://YuWanCard/images/relics/pig_carrot.png";

    private readonly List<CanvasItem> _merchantContentNodes = [];

    private NMerchantInventory? _inventory;
    private Button? _shopTabButton;
    private Button? _stationTabButton;
    private Control? _stationPanel;
    private Label? _tokenLabel;
    private OfferCardView? _offerCard1;
    private OfferCardView? _offerCard2;
    private Button? _refreshButton;

    private BalatroModifier? _modifier;
    private Player? _player;
    private bool _stationVisible;

    public void Initialize(NMerchantInventory inventory)
    {
        _inventory = inventory;
        Name = "YuWanBalatroMerchantExtension";
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 1;

        BuildUi();
        CacheMerchantNodes();
        ShowShop();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_inventory == null)
        {
            Visible = false;
            return;
        }

        bool shouldShow = _modifier != null
            && _inventory.IsOpen
            && _inventory.Visible
            && ActiveScreenContext.Instance.IsCurrent(_inventory);
        Visible = shouldShow;
        SyncModeVisibility();
    }

    public void RefreshForOpen()
    {
        RunState? state = RunManager.Instance?.State;
        _player = state == null ? null : LocalContext.GetMe(state) ?? state.Players.FirstOrDefault();
        _modifier = state == null ? null : BalatroModifier.GetInstance(state);

        bool active = _modifier != null && state?.CurrentRoom?.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Shop;
        Visible = active;
        if (!active)
        {
            ShowShop();
            return;
        }

        _modifier!.EnsureModStationOffers(_player!);
        UpdateUi();
    }

    public void OnInventoryClosed()
    {
        Visible = false;
        ShowShop();
    }

    private void BuildUi()
    {
        HBoxContainer tabRow = new()
        {
            Name = "BalatroMerchantTabs",
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 2,
            AnchorLeft = 0.5f,
            AnchorTop = 0f,
            AnchorRight = 0.5f,
            AnchorBottom = 0f,
            OffsetLeft = -182f,
            OffsetTop = TabRowTop,
            OffsetRight = 182f,
            OffsetBottom = TabRowTop + TabRowHeight,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        tabRow.AddThemeConstantOverride("separation", 12);
        AddChild(tabRow);

        _shopTabButton = CreateTabButton("YUWANCARD-BALATRO_MOD_STATION.shop_tab", primary: false);
        _shopTabButton.Pressed += ShowShop;
        tabRow.AddChild(_shopTabButton);

        _stationTabButton = CreateTabButton("YUWANCARD-BALATRO_MOD_STATION.station_tab", primary: true);
        _stationTabButton.Pressed += ShowStation;
        tabRow.AddChild(_stationTabButton);

        _stationPanel = new Control
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 1,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetTop = StationRootTop
        };
        AddChild(_stationPanel);

        MarginContainer bodyMargin = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        bodyMargin.AnchorLeft = 0f;
        bodyMargin.AnchorTop = 0f;
        bodyMargin.AnchorRight = 1f;
        bodyMargin.AnchorBottom = 1f;
        bodyMargin.OffsetLeft = StationSideMargin;
        bodyMargin.OffsetTop = 0f;
        bodyMargin.OffsetRight = -StationSideMargin;
        bodyMargin.OffsetBottom = -StationBottomMargin;
        bodyMargin.AddThemeConstantOverride("margin_left", 28);
        bodyMargin.AddThemeConstantOverride("margin_top", 6);
        bodyMargin.AddThemeConstantOverride("margin_right", 28);
        bodyMargin.AddThemeConstantOverride("margin_bottom", 12);
        _stationPanel.AddChild(bodyMargin);

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 18);
        bodyMargin.AddChild(root);

        VBoxContainer header = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);

        header.AddChild(CreateLocLabel("YUWANCARD-BALATRO_MOD_STATION.title", 32, BalatroUiTheme.Title));

        Label subtitle = CreateLocLabel("YUWANCARD-BALATRO_MOD_STATION.description", 18, BalatroUiTheme.Body);
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        header.AddChild(subtitle);

        _tokenLabel = BalatroUiTheme.CreateTextLabel(string.Empty, 20, BalatroUiTheme.Price);
        header.AddChild(_tokenLabel);

        HBoxContainer offersRow = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        offersRow.AddThemeConstantOverride("separation", 22);
        root.AddChild(offersRow);

        _offerCard1 = CreateOfferCard();
        _offerCard1.Button.Pressed += async () => await OnOfferPressed(0);
        offersRow.AddChild(_offerCard1.Button);

        _offerCard2 = CreateOfferCard();
        _offerCard2.Button.Pressed += async () => await OnOfferPressed(1);
        offersRow.AddChild(_offerCard2.Button);

        HBoxContainer bottomRow = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.End
        };
        root.AddChild(bottomRow);

        _refreshButton = new Button
        {
            Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.refresh").GetFormattedText(),
            CustomMinimumSize = new Vector2(220f, 46f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        BalatroUiTheme.ApplyActionButtonStyle(_refreshButton, primary: false);
        _refreshButton.AddThemeFontSizeOverride("font_size", 18);
        _refreshButton.Pressed += async () => await OnRefreshPressed();
        bottomRow.AddChild(_refreshButton);
    }

    private void CacheMerchantNodes()
    {
        if (_inventory == null)
        {
            return;
        }

        _merchantContentNodes.Clear();
        AddMerchantNode("%CharacterCards");
        AddMerchantNode("%ColorlessCards");
        AddMerchantNode("%Relics");
        AddMerchantNode("%Potions");
        AddMerchantNode("%MerchantCardRemoval");
    }

    private void AddMerchantNode(string path)
    {
        if (_inventory?.GetNodeOrNull<CanvasItem>(path) is { } node)
        {
            _merchantContentNodes.Add(node);
        }
    }

    private static Button CreateTabButton(string locKey, bool primary)
    {
        Button button = new()
        {
            Text = new LocString("gameplay_ui", locKey).GetFormattedText(),
            CustomMinimumSize = new Vector2(176f, 42f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        BalatroUiTheme.ApplyActionButtonStyle(button, primary);
        button.AddThemeFontSizeOverride("font_size", 17);
        return button;
    }

    private static OfferCardView CreateOfferCard()
    {
        Button button = new()
        {
            CustomMinimumSize = new Vector2(OfferCardWidth, OfferCardHeight),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        BalatroUiTheme.ApplyCardButtonStyle(button);

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
        layout.AddThemeConstantOverride("separation", 12);
        margin.AddChild(layout);

        PanelContainer iconFrame = BalatroUiTheme.CreateTextureIcon(
            GD.Load<Texture2D>(DefaultOfferIconPath),
            74f);
        layout.AddChild(iconFrame);

        Label iconLabel = BalatroUiTheme.CreateTextLabel(string.Empty, 16, BalatroUiTheme.Muted, HorizontalAlignment.Center);

        Label titleLabel = BalatroUiTheme.CreateTextLabel(string.Empty, 24, BalatroUiTheme.Title, HorizontalAlignment.Center);
        layout.AddChild(titleLabel);

        Label descriptionLabel = BalatroUiTheme.CreateTextLabel(string.Empty, 18, BalatroUiTheme.Body, HorizontalAlignment.Center, wrap: true);
        descriptionLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(descriptionLabel);

        Control spacer = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddChild(spacer);

        Label priceLabel = BalatroUiTheme.CreateTextLabel(string.Empty, 20, BalatroUiTheme.Price, HorizontalAlignment.Center, wrap: true);
        layout.AddChild(priceLabel);

        return new OfferCardView(button, iconFrame, iconLabel, titleLabel, descriptionLabel, priceLabel);
    }

    private static Label CreateLocLabel(string locKey, int fontSize, Color color)
    {
        return BalatroUiTheme.CreateTextLabel(new LocString("gameplay_ui", locKey).GetFormattedText(), fontSize, color);
    }

    private void ShowShop()
    {
        _stationVisible = false;
        SyncModeVisibility();
        UpdateTabState();
    }

    private void ShowStation()
    {
        if (_modifier == null || _player == null)
        {
            return;
        }

        _stationVisible = true;
        _modifier.EnsureModStationOffers(_player);
        SyncModeVisibility();
        UpdateUi();
    }

    private void UpdateUi()
    {
        SyncModeVisibility();
        UpdateTabState();
        if (_tokenLabel == null || _modifier == null || _player == null || _offerCard1 == null || _offerCard2 == null)
        {
            return;
        }

        _tokenLabel.Text = string.Format(
            new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.tokens").GetRawText(),
            _modifier.ModifierTokenCount);

        IReadOnlyList<BalatroCardEdition> offers = _modifier.GetModStationOffers();
        UpdateOfferCard(_offerCard1, offers[0]);
        UpdateOfferCard(_offerCard2, offers[1]);
        if (_refreshButton != null)
        {
            _refreshButton.Disabled = _player.Gold < 25;
        }
    }

    private void UpdateTabState()
    {
        if (_shopTabButton == null || _stationTabButton == null)
        {
            return;
        }

        _shopTabButton.Disabled = !_stationVisible;
        _stationTabButton.Disabled = _stationVisible;
    }

    private void UpdateOfferCard(OfferCardView card, BalatroCardEdition edition)
    {
        if (_modifier == null || _player == null)
        {
            return;
        }

        string editionKey = edition.ToString().ToUpperInvariant();
        string title = new LocString("gameplay_ui", $"YUWANCARD-BALATRO_EDITION.{editionKey}.title").GetFormattedText();
        string description = new LocString("gameplay_ui", $"YUWANCARD-BALATRO_EDITION.{editionKey}.description").GetFormattedText();
        Color accentColor = BalatroUiTheme.GetEditionAccent(edition);
        bool useToken = _modifier.ModifierTokenCount > 0;
        int cost = _modifier.GetEditionShopCost(edition);
        bool canApply = CanApplyAnyCard(edition);
        bool affordable = useToken || _player.Gold >= cost;

        card.IconFrame.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat
            {
                BgColor = new Color(accentColor.R, accentColor.G, accentColor.B, 0.14f),
                BorderColor = accentColor,
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomLeft = 12,
                CornerRadiusBottomRight = 12
            });
        card.IconLabel.Text = string.Empty;
        card.TitleLabel.Text = title;
        card.DescriptionLabel.Text = description;
        card.PriceLabel.Text = useToken
            ? new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.token_payment").GetFormattedText()
            : string.Format(new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.gold_payment").GetRawText(), cost);
        card.PriceLabel.AddThemeColorOverride("font_color", affordable ? BalatroUiTheme.Price : BalatroUiTheme.Muted);
        card.Button.TooltipText = $"{title}\n{description}";
        card.Button.Disabled = !canApply || !affordable;
    }

    private bool CanApplyAnyCard(BalatroCardEdition edition)
    {
        return _player?.Deck.Cards.Any(card => BalatroCardEditionHelper.CanApplyEdition(card, edition)) == true;
    }

    private async Task OnOfferPressed(int offerIndex)
    {
        if (_modifier == null || _player == null)
        {
            return;
        }

        BalatroCardEdition edition = _modifier.GetModStationOffers()[offerIndex];
        bool success = await RunWithMerchantHiddenAsync(() => _modifier.PurchaseModStationOffer(_player, edition));
        if (success)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
            UpdateUi();
        }
        else
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
        }
    }

    private async Task OnRefreshPressed()
    {
        if (_modifier == null || _player == null)
        {
            return;
        }

        if (await _modifier.RefreshModStationOffers(_player, payRefreshCost: true))
        {
            SfxCmd.Play("event:/sfx/ui/ui_card_reward_open");
            UpdateUi();
        }
        else
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
        }
    }

    private void SetMerchantContentVisible(bool visible)
    {
        foreach (CanvasItem node in _merchantContentNodes)
        {
            node.Visible = visible;
        }
    }

    private void SyncModeVisibility()
    {
        bool showStation = _stationVisible && Visible;
        SetMerchantContentVisible(!showStation);
        if (_stationPanel != null)
        {
            _stationPanel.Visible = showStation;
        }
    }

    private async Task<bool> RunWithMerchantHiddenAsync(Func<Task<bool>> action)
    {
        if (_inventory == null)
        {
            return await action();
        }

        bool inventoryVisible = _inventory.Visible;
        _inventory.Visible = false;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            return await action();
        }
        finally
        {
            _inventory.Visible = inventoryVisible;
            if (_stationVisible)
            {
                ShowStation();
            }
            else
            {
                ShowShop();
            }
        }
    }

    private sealed class OfferCardView
    {
        public OfferCardView(
            Button button,
            PanelContainer iconFrame,
            Label iconLabel,
            Label titleLabel,
            Label descriptionLabel,
            Label priceLabel)
        {
            Button = button;
            IconFrame = iconFrame;
            IconLabel = iconLabel;
            TitleLabel = titleLabel;
            DescriptionLabel = descriptionLabel;
            PriceLabel = priceLabel;
        }

        public Button Button { get; }
        public PanelContainer IconFrame { get; }
        public Label IconLabel { get; }
        public Label TitleLabel { get; }
        public Label DescriptionLabel { get; }
        public Label PriceLabel { get; }
    }
}
