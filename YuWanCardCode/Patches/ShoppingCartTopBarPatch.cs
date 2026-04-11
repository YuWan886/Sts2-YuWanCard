using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YuWanCard.UI;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NTopBar))]
public static class NTopBar_ShoppingCartPatch
{
    private static Button? _shoppingCartButton;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void AddShoppingCartButton(NTopBar __instance)
    {
        var mapButton = __instance.Map;
        if (mapButton == null) return;

        var parent = mapButton.GetParent();
        if (parent == null) return;

        var button = CreateShoppingCartButton();
        if (button == null) return;

        _shoppingCartButton = button;
        parent.AddChild(button);
        parent.MoveChild(button, mapButton.GetIndex());
        
        UpdateButtonVisibility();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Initialize")]
    public static void UpdateButtonVisibility()
    {
        if (_shoppingCartButton == null) return;

        var hasCart = ShoppingCartManager.HasShoppingCart();
        _shoppingCartButton.Visible = hasCart;
    }

    public static void RefreshButtonVisibility() => UpdateButtonVisibility();

    private static Button CreateShoppingCartButton()
    {
        var button = new Button
        {
            Name = "ShoppingCartButton",
            CustomMinimumSize = new Vector2(80, 80)
        };
        button.AddThemeFontSizeOverride("font_size", 24);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0, 0, 0, 0);
        styleBox.BorderColor = new Color(0, 0, 0, 0);
        button.AddThemeStyleboxOverride("normal", styleBox);
        button.AddThemeStyleboxOverride("hover", styleBox);
        button.AddThemeStyleboxOverride("pressed", styleBox);

        var iconPath = "res://YuWanCard/images/ui/shopping_cart_icon.png";
        var icon = GD.Load<Texture2D>(iconPath);
        if (icon != null)
        {
            var iconRect = new TextureRect
            {
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                CustomMinimumSize = new Vector2(60, 60),
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -30,
                OffsetTop = -30,
                OffsetRight = 30,
                OffsetBottom = 30
            };
            button.AddChild(iconRect);
        }

        button.Pressed += OnShoppingCartButtonPressed;
        return button;
    }

    private static void OnShoppingCartButtonPressed()
    {
        if (!ShoppingCartManager.HasShoppingCart()) return;
        var popup = ShoppingCartPopup.Create();
        popup?.Open();
    }
}
