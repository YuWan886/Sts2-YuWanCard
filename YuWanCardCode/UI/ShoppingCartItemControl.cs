using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Utils;

namespace YuWanCard.UI;

public partial class ShoppingCartItemControl : PanelContainer
{
    private ShoppingCartItem _item = null!;
    private int _index;
    private ShoppingCartPopup _popup = null!;

    public ShoppingCartItemControl()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, 80);
        
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.13f, 0.18f, 0.8f),
            BorderColor = new Color(0.3f, 0.28f, 0.35f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        AddThemeStyleboxOverride("panel", styleBox);
    }

    public void Initialize(ShoppingCartItem item, int index, ShoppingCartPopup popup)
    {
        _item = item;
        _index = index;
        _popup = popup;
    }

    public override void _Ready()
    {
        var mainContainer = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        AddChild(mainContainer);

        var iconTexture = GetItemIcon();
        if (iconTexture != null)
        {
            var iconRect = new TextureRect
            {
                Texture = iconTexture,
                CustomMinimumSize = new Vector2(64, 64),
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
            };
            mainContainer.AddChild(iconRect);
        }

        var infoContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        mainContainer.AddChild(infoContainer);

        var nameRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        infoContainer.AddChild(nameRow);

        var nameLabel = new Label
        {
            Text = GetItemName(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 24);
        nameLabel.AddThemeColorOverride("font_color", GetItemColor());
        nameRow.AddChild(nameLabel);

        var priceLabel = new Label { Text = GetPriceText() };
        priceLabel.AddThemeFontSizeOverride("font_size", 22);
        priceLabel.AddThemeColorOverride("font_color", CanAfford() ? new Color(1f, 0.85f, 0.5f) : new Color(1f, 0.3f, 0.3f));
        nameRow.AddChild(priceLabel);

        var typeLabel = new Label { Text = GetItemTypeText() };
        typeLabel.AddThemeFontSizeOverride("font_size", 18);
        typeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        infoContainer.AddChild(typeLabel);

        var buttonRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        infoContainer.AddChild(buttonRow);

        var buyButton = CreateButton(GetLocText("YUWANCARD-SHOPPING_CART.buy"));
        buyButton.Pressed += OnBuyPressed;
        buttonRow.AddChild(buyButton);

        var removeButton = CreateButton(GetLocText("YUWANCARD-SHOPPING_CART.remove"));
        removeButton.Pressed += OnRemovePressed;
        buttonRow.AddChild(removeButton);
    }

    private static Button CreateButton(string text)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.AddThemeFontSizeOverride("font_size", 22);
        return button;
    }

    private bool CanAfford()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = runState != null ? LocalContext.GetMe(runState.Players) : null;
        return player != null && player.Gold >= _item.Price;
    }

    private string GetPriceText() => _item.IsOnSale ? $"{_item.Price}G (SALE)" : $"{_item.Price}G";

    private string GetItemName() => _item.ItemType switch
    {
        ShoppingCartItemType.Card => ShoppingCartManager.GetCardModel(_item)?.Title ?? _item.ItemId,
        ShoppingCartItemType.Relic => ShoppingCartManager.GetRelicModel(_item)?.Title?.GetFormattedText() ?? _item.ItemId,
        ShoppingCartItemType.Potion => ShoppingCartManager.GetPotionModel(_item)?.Title?.GetFormattedText() ?? _item.ItemId,
        _ => _item.ItemId
    };

    private string GetItemTypeText() => _item.ItemType switch
    {
        ShoppingCartItemType.Card => GetLocText("YUWANCARD-SHOPPING_CART.type_card"),
        ShoppingCartItemType.Relic => GetLocText("YUWANCARD-SHOPPING_CART.type_relic"),
        ShoppingCartItemType.Potion => GetLocText("YUWANCARD-SHOPPING_CART.type_potion"),
        _ => ""
    };

    private Color GetItemColor() => _item.ItemType switch
    {
        ShoppingCartItemType.Card => new Color(0.9f, 0.7f, 0.3f),
        ShoppingCartItemType.Relic => new Color(0.7f, 0.6f, 0.9f),
        ShoppingCartItemType.Potion => new Color(0.3f, 0.8f, 0.6f),
        _ => Colors.White
    };

    private Texture2D? GetItemIcon() => _item.ItemType switch
    {
        ShoppingCartItemType.Card => ShoppingCartManager.GetCardModel(_item)?.Portrait,
        ShoppingCartItemType.Relic => ShoppingCartManager.GetRelicModel(_item)?.BigIcon,
        ShoppingCartItemType.Potion => ShoppingCartManager.GetPotionModel(_item)?.Image,
        _ => null
    };

    private async void OnBuyPressed() => await _popup.TryPurchaseItem(_index);
    private void OnRemovePressed() => _popup.RemoveItem(_index);
    private static string GetLocText(string key) => new LocString("settings_ui", key).GetRawText();
}
