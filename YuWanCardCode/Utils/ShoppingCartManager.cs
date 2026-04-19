using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.GameActions;
using YuWanCard.Relics;

namespace YuWanCard.Utils;

public static class ShoppingCartManager
{
    public static bool IsMultiplayerGame
    {
        get
        {
            var runManager = RunManager.Instance;
            if (runManager == null || !runManager.IsInProgress)
                return false;
            return runManager.NetService.Type.IsMultiplayer();
        }
    }

    public static ShoppingCart? GetShoppingCartRelic(Player? player = null)
    {
        if (player == null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return null;
            player = LocalContext.GetMe(runState.Players);
        }
        if (player == null)
            return null;

        foreach (var relic in player.Relics)
        {
            if (relic is ShoppingCart cart)
                return cart;
        }
        return null;
    }

    public static bool HasShoppingCart(Player? player = null)
    {
        return GetShoppingCartRelic(player) != null;
    }

    public static ShoppingCartData? GetCartData(Player? player = null)
    {
        var cart = GetShoppingCartRelic(player);
        return cart?.GetCartData();
    }

    public static bool AddToCart(MerchantCardEntry cardEntry, Player? player = null)
    {
        var data = GetCartData(player);
        if (data == null)
        {
            MainFile.Logger.Warn("ShoppingCartManager: No shopping cart found");
            return false;
        }

        if (cardEntry.CreationResult?.Card == null)
        {
            MainFile.Logger.Warn("ShoppingCartManager: Card entry has no card");
            return false;
        }

        var item = new ShoppingCartItem(cardEntry);
        return data.AddItem(item);
    }

    public static bool AddToCart(MerchantRelicEntry relicEntry, Player? player = null)
    {
        var data = GetCartData(player);
        if (data == null)
        {
            MainFile.Logger.Warn("ShoppingCartManager: No shopping cart found");
            return false;
        }

        if (relicEntry.Model == null)
        {
            MainFile.Logger.Warn("ShoppingCartManager: Relic entry has no model");
            return false;
        }

        var item = new ShoppingCartItem(relicEntry);
        return data.AddItem(item);
    }

    public static bool AddToCart(MerchantPotionEntry potionEntry, Player? player = null)
    {
        var data = GetCartData(player);
        if (data == null)
        {
            MainFile.Logger.Warn("ShoppingCartManager: No shopping cart found");
            return false;
        }

        if (potionEntry.Model == null)
        {
            MainFile.Logger.Warn("ShoppingCartManager: Potion entry has no model");
            return false;
        }

        var item = new ShoppingCartItem(potionEntry);
        return data.AddItem(item);
    }

    public static bool RemoveFromCart(int index, Player? player = null)
    {
        var data = GetCartData(player);
        if (data == null)
            return false;

        return data.RemoveAt(index);
    }

    public static bool RemoveFromCart(ShoppingCartItem item, Player? player = null)
    {
        var data = GetCartData(player);
        if (data == null)
            return false;

        return data.RemoveItem(item);
    }

    public static async Task<bool> PurchaseItem(int index, Player? player = null)
    {
        if (player == null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState != null)
                player = LocalContext.GetMe(runState.Players);
        }

        if (player == null)
            return false;

        var data = GetCartData(player);
        if (data == null)
            return false;

        var item = data.GetItem(index);
        if (item == null)
            return false;

        if (player.Gold < item.Price)
        {
            MainFile.Logger.Warn($"ShoppingCartManager: Not enough gold ({player.Gold} < {item.Price})");
            return false;
        }

        if (IsMultiplayerGame)
        {
            var action = new ShoppingCartPurchaseAction(player, index);
            var synchronizer = RunManager.Instance.ActionQueueSynchronizer;
            synchronizer.RequestEnqueue(action);
            return true;
        }

        bool success = false;

        switch (item.ItemType)
        {
            case ShoppingCartItemType.Card:
                success = await PurchaseCard(item, player);
                break;
            case ShoppingCartItemType.Relic:
                success = await PurchaseRelic(item, player);
                break;
            case ShoppingCartItemType.Potion:
                success = await PurchasePotion(item, player);
                break;
        }

        if (success)
        {
            data.RemoveAt(index);
            var cart = GetShoppingCartRelic(player);
            cart?.SaveCartData();
        }

        return success;
    }

    internal static async Task<bool> PurchaseCard(ShoppingCartItem item, Player player)
    {
        if (item.ModelId == null)
            return false;

        var cardModel = ModelDb.GetByIdOrNull<CardModel>(item.ModelId);
        if (cardModel == null)
        {
            MainFile.Logger.Warn($"ShoppingCartManager: Card not found: {item.ItemId}");
            return false;
        }

        var mutableCard = player.RunState.CreateCard(cardModel, player);

        var result = await CardPileCmd.Add(mutableCard, PileType.Deck);
        if (!result.success)
        {
            MainFile.Logger.Warn("ShoppingCartManager: Failed to add card to deck");
            return false;
        }

        await PlayerCmd.LoseGold(item.Price, player, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);

        MainFile.Logger.Info($"ShoppingCartManager: Purchased card {item.ItemId} for {item.Price} gold");
        return true;
    }

    internal static async Task<bool> PurchaseRelic(ShoppingCartItem item, Player player)
    {
        if (item.ModelId == null)
            return false;

        var relicModel = ModelDb.GetByIdOrNull<RelicModel>(item.ModelId);
        if (relicModel == null)
        {
            MainFile.Logger.Warn($"ShoppingCartManager: Relic not found: {item.ItemId}");
            return false;
        }

        var mutableRelic = relicModel.ToMutable();

        await RelicCmd.Obtain(mutableRelic, player);
        await PlayerCmd.LoseGold(item.Price, player, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);

        MainFile.Logger.Info($"ShoppingCartManager: Purchased relic {item.ItemId} for {item.Price} gold");
        return true;
    }

    internal static async Task<bool> PurchasePotion(ShoppingCartItem item, Player player)
    {
        if (item.ModelId == null)
            return false;

        var potionModel = ModelDb.GetByIdOrNull<PotionModel>(item.ModelId);
        if (potionModel == null)
        {
            MainFile.Logger.Warn($"ShoppingCartManager: Potion not found: {item.ItemId}");
            return false;
        }

        var mutablePotion = potionModel.ToMutable();

        if (!player.HasOpenPotionSlots)
        {
            MainFile.Logger.Warn("ShoppingCartManager: No potion slot available");
            return false;
        }

        var result = await PotionCmd.TryToProcure(mutablePotion, player);
        if (!result.success)
        {
            MainFile.Logger.Warn("ShoppingCartManager: Failed to obtain potion");
            return false;
        }

        await PlayerCmd.LoseGold(item.Price, player, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);

        MainFile.Logger.Info($"ShoppingCartManager: Purchased potion {item.ItemId} for {item.Price} gold");
        return true;
    }

    public static void ClearCart(Player? player = null)
    {
        var data = GetCartData(player);
        data?.Clear();

        var cart = GetShoppingCartRelic(player);
        cart?.SaveCartData();
    }

    public static bool CanAffordItem(ShoppingCartItem item, Player? player = null)
    {
        if (player == null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState != null)
                player = LocalContext.GetMe(runState.Players);
        }
        if (player == null)
            return false;

        return player.Gold >= item.Price;
    }

    public static CardModel? GetCardModel(ShoppingCartItem item)
    {
        if (item.ItemType != ShoppingCartItemType.Card || item.ModelId == null)
            return null;

        return ModelDb.GetByIdOrNull<CardModel>(item.ModelId);
    }

    public static RelicModel? GetRelicModel(ShoppingCartItem item)
    {
        if (item.ItemType != ShoppingCartItemType.Relic || item.ModelId == null)
            return null;

        return ModelDb.GetByIdOrNull<RelicModel>(item.ModelId);
    }

    public static PotionModel? GetPotionModel(ShoppingCartItem item)
    {
        if (item.ItemType != ShoppingCartItemType.Potion || item.ModelId == null)
            return null;

        return ModelDb.GetByIdOrNull<PotionModel>(item.ModelId);
    }
}
