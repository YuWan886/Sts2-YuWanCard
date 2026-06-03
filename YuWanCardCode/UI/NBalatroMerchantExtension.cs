using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NBalatroMerchantExtension : Control
{
    private const float TabRowTop = 96f;
    private const float TabRowHeight = 44f;
    private const float StationRootTop = 150f;
    private const float StationHeaderSideMargin = 112f;
    private const float StationOffersTop = 152f;
    private const float StationOffersSideMargin = 104f;
    private const float StationFooterBottom = 48f;

    private readonly List<CanvasItem> _merchantContentNodes = [];

    private NMerchantInventory? _inventory;
    private Button? _shopTabButton;
    private Button? _stationTabButton;
    private Control? _stationPanel;
    private Label? _tokenLabel;
    private Button? _offerButton1;
    private Button? _offerButton2;
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

        BuildUi();
        CacheMerchantNodes();
        ShowShop();
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

    private void BuildUi()
    {
        HBoxContainer tabRow = new()
        {
            Name = "BalatroMerchantTabs",
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 200,
            AnchorLeft = 0.5f,
            AnchorTop = 0f,
            AnchorRight = 0.5f,
            AnchorBottom = 0f,
            OffsetLeft = -180f,
            OffsetTop = TabRowTop,
            OffsetRight = 180f,
            OffsetBottom = TabRowTop + TabRowHeight,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        tabRow.AddThemeConstantOverride("separation", 12);
        AddChild(tabRow);

        _shopTabButton = CreateTabButton("YUWANCARD-BALATRO_MOD_STATION.shop_tab");
        _shopTabButton.Pressed += ShowShop;
        tabRow.AddChild(_shopTabButton);

        _stationTabButton = CreateTabButton("YUWANCARD-BALATRO_MOD_STATION.station_tab");
        _stationTabButton.Pressed += ShowStation;
        tabRow.AddChild(_stationTabButton);

        _stationPanel = new Control
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 199,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetTop = StationRootTop
        };
        AddChild(_stationPanel);

        VBoxContainer header = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        header.AnchorLeft = 0f;
        header.AnchorTop = 0f;
        header.AnchorRight = 1f;
        header.AnchorBottom = 0f;
        header.OffsetLeft = StationHeaderSideMargin;
        header.OffsetTop = 0f;
        header.OffsetRight = -StationHeaderSideMargin;
        header.OffsetBottom = 116f;
        header.AddThemeConstantOverride("separation", 10);
        _stationPanel.AddChild(header);

        Label title = CreateLabel("YUWANCARD-BALATRO_MOD_STATION.title", 28, new Color(1f, 0.89f, 0.66f));
        header.AddChild(title);

        Label subtitle = CreateLabel("YUWANCARD-BALATRO_MOD_STATION.description", 16, new Color(0.92f, 0.92f, 0.92f));
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        header.AddChild(subtitle);

        _tokenLabel = CreateLabel(string.Empty, 18, new Color(0.93f, 0.84f, 0.42f));
        header.AddChild(_tokenLabel);

        HBoxContainer offersRow = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        offersRow.AnchorLeft = 0f;
        offersRow.AnchorTop = 0f;
        offersRow.AnchorRight = 1f;
        offersRow.AnchorBottom = 0f;
        offersRow.OffsetLeft = StationOffersSideMargin;
        offersRow.OffsetTop = StationOffersTop;
        offersRow.OffsetRight = -StationOffersSideMargin;
        offersRow.OffsetBottom = StationOffersTop + 240f;
        offersRow.AddThemeConstantOverride("separation", 36);
        _stationPanel.AddChild(offersRow);

        _offerButton1 = CreateOfferButton();
        _offerButton1.Pressed += async () => await OnOfferPressed(0);
        offersRow.AddChild(_offerButton1);

        _offerButton2 = CreateOfferButton();
        _offerButton2.Pressed += async () => await OnOfferPressed(1);
        offersRow.AddChild(_offerButton2);

        HBoxContainer bottomRow = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.End
        };
        bottomRow.AnchorLeft = 0f;
        bottomRow.AnchorTop = 1f;
        bottomRow.AnchorRight = 1f;
        bottomRow.AnchorBottom = 1f;
        bottomRow.OffsetLeft = StationOffersSideMargin;
        bottomRow.OffsetTop = -StationFooterBottom - 44f;
        bottomRow.OffsetRight = -StationOffersSideMargin;
        bottomRow.OffsetBottom = -StationFooterBottom;
        _stationPanel.AddChild(bottomRow);

        _refreshButton = new Button
        {
            Text = new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.refresh").GetFormattedText(),
            CustomMinimumSize = new Vector2(220f, 44f),
            FocusMode = FocusModeEnum.None
        };
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

    private Button CreateTabButton(string locKey)
    {
        return new Button
        {
            Text = new LocString("gameplay_ui", locKey).GetFormattedText(),
            CustomMinimumSize = new Vector2(168f, 40f),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
    }

    private Button CreateOfferButton()
    {
        Button button = new()
        {
            CustomMinimumSize = new Vector2(360f, 220f),
            Alignment = HorizontalAlignment.Left,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop
        };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.AddThemeStyleboxOverride("normal", CreateOfferButtonStyle(new Color(0.12f, 0.1f, 0.08f, 0.72f)));
        button.AddThemeStyleboxOverride("hover", CreateOfferButtonStyle(new Color(0.16f, 0.13f, 0.1f, 0.82f)));
        button.AddThemeStyleboxOverride("pressed", CreateOfferButtonStyle(new Color(0.09f, 0.08f, 0.07f, 0.86f)));
        button.AddThemeStyleboxOverride("disabled", CreateOfferButtonStyle(new Color(0.08f, 0.08f, 0.08f, 0.45f)));
        return button;
    }

    private static StyleBoxFlat CreateOfferButtonStyle(Color bgColor)
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = new Color(0.95f, 0.83f, 0.58f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ContentMarginLeft = 18f,
            ContentMarginTop = 16f,
            ContentMarginRight = 18f,
            ContentMarginBottom = 16f
        };
    }

    private static Label CreateLabel(string locKey, int fontSize, Color color)
    {
        Label label = new()
        {
            Text = string.IsNullOrWhiteSpace(locKey)
                ? string.Empty
                : new LocString("gameplay_ui", locKey).GetFormattedText(),
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private void ShowShop()
    {
        _stationVisible = false;
        SetMerchantContentVisible(true);
        if (_stationPanel != null)
        {
            _stationPanel.Visible = false;
        }

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
        SetMerchantContentVisible(false);
        if (_stationPanel != null)
        {
            _stationPanel.Visible = true;
        }

        UpdateUi();
    }

    private void UpdateUi()
    {
        UpdateTabState();
        if (_tokenLabel == null || _modifier == null || _player == null)
        {
            return;
        }

        _tokenLabel.Text = string.Format(
            new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.tokens").GetRawText(),
            _modifier.ModifierTokenCount);

        IReadOnlyList<BalatroCardEdition> offers = _modifier.GetModStationOffers();
        UpdateOfferButton(_offerButton1, offers[0]);
        UpdateOfferButton(_offerButton2, offers[1]);
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

    private void UpdateOfferButton(Button? button, BalatroCardEdition edition)
    {
        if (button == null || _modifier == null || _player == null)
        {
            return;
        }

        string title = new LocString("gameplay_ui", $"YUWANCARD-BALATRO_EDITION.{edition.ToString().ToUpperInvariant()}.title").GetFormattedText();
        string description = new LocString("gameplay_ui", $"YUWANCARD-BALATRO_EDITION.{edition.ToString().ToUpperInvariant()}.description").GetFormattedText();
        bool useToken = _modifier.ModifierTokenCount > 0;
        int cost = _modifier.GetEditionShopCost(edition);
        string paymentText = useToken
            ? new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.token_payment").GetFormattedText()
            : string.Format(new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.gold_payment").GetRawText(), cost);

        button.Text = $"{title}\n{description}\n{paymentText}";
        button.TooltipText = $"{title}\n{description}";
        button.Disabled = !CanApplyAnyCard(edition) || (!useToken && _player.Gold < cost);
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
}
