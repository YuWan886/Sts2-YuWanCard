using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.UI;

public partial class ShoppingCartPopup : Control, IScreenContext
{
    private PanelContainer? _mainPanel;
    private VBoxContainer? _itemContainer;
    private Label? _emptyLabel;
    private Label? _totalLabel;
    private Button? _buyAllButton;

    private Player? _player;
    private ShoppingCart? _cart;
    private ShoppingCartData? _subscribedData;
    private List<ShoppingCartItemControl> _itemControls = new();

    public Control? DefaultFocusedControl => null;

    public static ShoppingCartPopup? Create()
    {
        var popup = new ShoppingCartPopup();
        popup.SetAnchorsPreset(LayoutPreset.FullRect);
        popup.MouseFilter = MouseFilterEnum.Ignore;
        popup.SetupUI();
        return popup;
    }

    private void SetupUI()
    {
        _mainPanel = new PanelContainer();
        _mainPanel.AnchorLeft = 0.5f;
        _mainPanel.AnchorRight = 0.5f;
        _mainPanel.AnchorTop = 0.5f;
        _mainPanel.AnchorBottom = 0.5f;
        _mainPanel.OffsetLeft = -350f;
        _mainPanel.OffsetRight = 350f;
        _mainPanel.OffsetTop = -400f;
        _mainPanel.OffsetBottom = 400f;

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        styleBox.BorderColor = new Color(0.4f, 0.35f, 0.25f);
        styleBox.SetBorderWidthAll(3);
        styleBox.SetCornerRadiusAll(10);
        styleBox.SetContentMarginAll(15);
        _mainPanel.AddThemeStyleboxOverride("panel", styleBox);

        var mainVBox = new VBoxContainer();
        mainVBox.Alignment = BoxContainer.AlignmentMode.Center;
        _mainPanel.AddChild(mainVBox);

        var titleContainer = new HBoxContainer();
        titleContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(titleContainer);

        var leftSpacer = new Control();
        leftSpacer.CustomMinimumSize = new Vector2(40, 40);
        leftSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleContainer.AddChild(leftSpacer);

        var titleLabel = CreateLabel(GetLocText("YUWANCARD-SHOPPING_CART.title"), true);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleContainer.AddChild(titleLabel);

        var rightSpacer = new Control();
        rightSpacer.CustomMinimumSize = new Vector2(40, 40);
        rightSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleContainer.AddChild(rightSpacer);

        var closeButton = new Button();
        closeButton.Text = "X";
        closeButton.AddThemeFontSizeOverride("font_size", 28);
        closeButton.CustomMinimumSize = new Vector2(40, 40);
        closeButton.Pressed += Close;
        titleContainer.AddChild(closeButton);

        mainVBox.AddChild(new HSeparator());

        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(scrollContainer);

        _itemContainer = new VBoxContainer();
        _itemContainer.Alignment = BoxContainer.AlignmentMode.Center;
        _itemContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scrollContainer.AddChild(_itemContainer);

        _emptyLabel = CreateLabel(GetLocText("YUWANCARD-SHOPPING_CART.empty"), false);
        _emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _itemContainer.AddChild(_emptyLabel);

        mainVBox.AddChild(new HSeparator());

        var bottomContainer = new HBoxContainer();
        bottomContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bottomContainer.Alignment = BoxContainer.AlignmentMode.Center;
        bottomContainer.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        mainVBox.AddChild(bottomContainer);

        var bottomLeftSpacer = new Control();
        bottomLeftSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bottomContainer.AddChild(bottomLeftSpacer);

        _totalLabel = CreateLabel("", false);
        _totalLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _totalLabel.VerticalAlignment = VerticalAlignment.Center;
        _totalLabel.CustomMinimumSize = new Vector2(150, 40);
        bottomContainer.AddChild(_totalLabel);

        _buyAllButton = new Button();
        _buyAllButton.Text = GetLocText("YUWANCARD-SHOPPING_CART.buy_all");
        _buyAllButton.AddThemeFontSizeOverride("font_size", 20);
        _buyAllButton.CustomMinimumSize = new Vector2(150, 40);
        _buyAllButton.Pressed += OnBuyAllPressed;
        bottomContainer.AddChild(_buyAllButton);

        var bottomRightSpacer = new Control();
        bottomRightSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bottomContainer.AddChild(bottomRightSpacer);

        AddChild(_mainPanel);

        InitializeData();
    }

    private Label CreateLabel(string text, bool isTitle)
    {
        var label = new Label();
        label.Text = text;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        if (isTitle)
        {
            label.AddThemeFontSizeOverride("font_size", 36);
            label.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.5f));
        }
        else
        {
            label.AddThemeFontSizeOverride("font_size", 28);
        }

        return label;
    }

    private void InitializeData()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null)
            _player = LocalContext.GetMe(runState.Players);
        _cart = ShoppingCartManager.GetShoppingCartRelic(_player);

        UnsubscribeEvents();

        if (_cart != null)
        {
            _subscribedData = _cart.GetCartData();
            _subscribedData.ItemAdded += RefreshItems;
            _subscribedData.ItemRemoved += RefreshItems;
            _subscribedData.CartCleared += RefreshItems;
        }

        RefreshItems();
    }

    private void UnsubscribeEvents()
    {
        if (_subscribedData != null)
        {
            _subscribedData.ItemAdded -= RefreshItems;
            _subscribedData.ItemRemoved -= RefreshItems;
            _subscribedData.CartCleared -= RefreshItems;
            _subscribedData = null;
        }
    }

    private void RefreshItems()
    {
        if (!IsInstanceValid(this))
            return;

        foreach (var control in _itemControls)
        {
            if (IsInstanceValid(control))
                control.QueueFree();
        }
        _itemControls.Clear();

        var data = ShoppingCartManager.GetCartData(_player);
        if (data == null || data.IsEmpty)
        {
            if (_emptyLabel != null && IsInstanceValid(_emptyLabel))
                _emptyLabel.Visible = true;
            if (_totalLabel != null && IsInstanceValid(_totalLabel))
                _totalLabel.Text = "";
            if (_buyAllButton != null && IsInstanceValid(_buyAllButton))
                _buyAllButton.Visible = false;
            return;
        }

        if (_emptyLabel != null && IsInstanceValid(_emptyLabel))
            _emptyLabel.Visible = false;

        for (int i = 0; i < data.Count; i++)
        {
            var item = data.GetItem(i);
            if (item != null)
            {
                var control = new ShoppingCartItemControl();
                control.Initialize(item, i, this);
                _itemControls.Add(control);
                if (_itemContainer != null && IsInstanceValid(_itemContainer))
                    _itemContainer.AddChild(control);
            }
        }

        if (_totalLabel != null && IsInstanceValid(_totalLabel))
        {
            _totalLabel.Text = GetLocText("YUWANCARD-SHOPPING_CART.total").Replace("{0}", data.TotalPrice.ToString());
        }

        if (_buyAllButton != null && IsInstanceValid(_buyAllButton))
        {
            _buyAllButton.Visible = true;
            _buyAllButton.Disabled = _player != null && _player.Gold < data.TotalPrice;
        }
    }

    private void RefreshItems(ShoppingCartItem? _ = null)
    {
        RefreshItems();
    }

    public void Open()
    {
        NModalContainer.Instance?.Add(this, showBackstop: true);
        SfxCmd.Play("event:/sfx/ui/ui_card_reward_open");
    }

    public void Close()
    {
        NModalContainer.Instance?.Clear();
        SfxCmd.Play("event:/sfx/ui/ui_button_click");
    }

    public async Task<bool> TryPurchaseItem(int index)
    {
        var data = ShoppingCartManager.GetCartData(_player);
        if (data == null)
            return false;

        var item = data.GetItem(index);
        if (item == null)
            return false;

        if (_player != null && _player.Gold < item.Price)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
            return false;
        }

        var success = await ShoppingCartManager.PurchaseItem(index, _player);
        if (success)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
            RefreshItems();
        }
        else
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
        }

        return success;
    }

    public void RemoveItem(int index)
    {
        ShoppingCartManager.RemoveFromCart(index, _player);
        SfxCmd.Play("event:/sfx/ui/ui_card_remove");
        RefreshItems();
    }

    private async void OnBuyAllPressed()
    {
        var data = ShoppingCartManager.GetCartData(_player);
        if (data == null || data.IsEmpty) return;

        if (_player != null && _player.Gold < data.TotalPrice)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
            return;
        }

        int purchasedCount = 0;
        for (int i = data.Count - 1; i >= 0; i--)
        {
            var success = await ShoppingCartManager.PurchaseItem(i, _player);
            if (success) purchasedCount++;
        }

        if (purchasedCount > 0)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
            RefreshItems();
        }
        else
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_dissapointment");
        }
    }

    private static string GetLocText(string key) => new LocString("settings_ui", key).GetRawText();

    public override void _ExitTree()
    {
        base._ExitTree();
        UnsubscribeEvents();
    }
}
