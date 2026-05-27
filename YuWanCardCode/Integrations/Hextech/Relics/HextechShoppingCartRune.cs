using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class HextechShoppingCartRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Prismatic;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DiscountPercent", 20)
    ];

    public override bool IsAvailableForPlayer(Player player)
    {
        return base.IsAvailableForPlayer(player) && player.GetRelic<ShoppingCart>() != null;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ApplyCartDiscount();
    }

    public override Task BeforeCombatStart()
    {
        ApplyCartDiscount();
        return Task.CompletedTask;
    }

    private void ApplyCartDiscount()
    {
        if (Owner == null)
        {
            return;
        }

        var cart = ShoppingCartManager.GetShoppingCartRelic(Owner);
        if (cart != null)
        {
            Flash();
            cart.GetCartData().DiscountMultiplier = 0.8;
            MainFile.Logger.Info($"HextechShoppingCartRune: Applied 20% discount to shopping cart");
        }
    }
}
