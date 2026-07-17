using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Config;
using YuWanCard.Hextech;
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
            var netService = runManager.NetService;
            return netService != null && netService.Type != NetGameType.Singleplayer && netService.Type != NetGameType.Replay;
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

    public static bool IsPurchaseBlockedInCurrentRoom(Player? player = null)
    {
        if (player == null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState != null)
                player = LocalContext.GetMe(runState.Players);
        }

        return player?.RunState.CurrentRoom is CombatRoom;
    }

    public static ShoppingCartData? GetCartData(Player? player = null)
    {
        var cart = GetShoppingCartRelic(player);
        return cart?.GetCartData();
    }

    public static bool IsEntryReserved(MerchantEntry? entry)
    {
        if (entry == null)
        {
            return false;
        }

        string reservationKey = ShoppingCartItem.CreateReservationKey(entry);
        var modelId = GetEntryModelId(entry);
        return IsReservationConsumed(reservationKey) || (modelId != null && IsItemReserved(modelId, reservationKey));
    }

    public static bool IsItemReserved(ModelId? modelId, string? reservationKey = null)
    {
        if (modelId == null)
        {
            return false;
        }

        var runState = RunManager.Instance?.State;
        if (runState == null)
        {
            return false;
        }

        foreach (var player in runState.Players)
        {
            var cartData = GetCartData(player);
            if (cartData == null || cartData.IsEmpty)
            {
                continue;
            }

            foreach (var item in cartData.Items)
            {
                if (item.ModelId == null || !item.ModelId.Equals(modelId))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(reservationKey) && !string.IsNullOrEmpty(item.ReservationKey))
                {
                    if (item.ReservationKey == reservationKey)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanAddToCart(MerchantEntry? entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry => cardEntry.CreationResult?.Card != null,
            MerchantRelicEntry relicEntry => CanStoreRelicInCart(relicEntry.Model),
            MerchantPotionEntry potionEntry => potionEntry.Model != null,
            _ => false
        };
    }

    public static bool CanStoreRelicInCart(RelicModel? relicModel)
    {
        if (relicModel == null)
        {
            return false;
        }

        if (HextechForgeRegistry.TryGetRarity(relicModel, out _))
        {
            return false;
        }

        return relicModel.GetType().FullName != "HextechRunes.RandomForgeShopRelic";
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

        if (!CanStoreRelicInCart(relicEntry.Model))
        {
            MainFile.Logger.Info($"ShoppingCartManager: Relic {relicEntry.Model.Id.Entry} cannot be stored in shopping cart");
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

        if (IsPurchaseBlockedInCurrentRoom(player))
        {
            MainFile.Logger.Warn("ShoppingCartManager: Purchase blocked in combat room to avoid multiplayer desync");
            return false;
        }

        if (player.Gold < item.Price)
        {
            MainFile.Logger.Warn($"ShoppingCartManager: Not enough gold ({player.Gold} < {item.Price})");
            return false;
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
            var cart = GetShoppingCartRelic(player);
            cart?.MarkReservationConsumed(item.ReservationKey);
            data.RemoveAt(index);
            cart?.SaveCartData();
            MainFile.Logger.Info($"ShoppingCartManager: Purchased {item.ItemId} for {item.Price} gold");
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

        if (!YuWanContentAvailability.IsCardEnabled(cardModel))
        {
            MainFile.Logger.Info($"ShoppingCartManager: Blocked disabled colorless card purchase {item.ItemId}");
            return false;
        }

        var mutableCard = player.RunState.CreateCard(cardModel, player);

        var result = await CardPileCmd.Add(mutableCard, PileType.Deck);
        if (!result.success)
        {
            MainFile.Logger.Warn("ShoppingCartManager: Failed to add card to deck");
            return false;
        }

        CardCmd.PreviewCardPileAdd(result);

        await PlayerCmd.LoseGold(item.Price, player, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);
        RunManager.Instance.RewardSynchronizer.SyncLocalGoldLost(item.Price);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedCard(mutableCard);

        if (cardModel.Pool is ColorlessCardPool)
        {
            player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).BoughtColorless.Add(mutableCard.Id);
        }

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

        if (!CanStoreRelicInCart(relicModel))
        {
            MainFile.Logger.Warn($"ShoppingCartManager: Relic {relicModel.Id.Entry} cannot be purchased from shopping cart");
            return false;
        }

        // Resolve shop proxy relics (e.g. RandomForgeShopRelic) into actual obtainable relics.
        // Shop proxy relics exist only in the merchant and should not be obtained directly.
        // The resolver (registered by HextechRuntimeCompat) detects proxy types and generates
        // the actual relic. If no resolver is registered or the model is not a proxy, this is a no-op.
        if (ResolveShopProxyRelic != null)
        {
            var resolved = await ResolveShopProxyRelic(relicModel, player);
            if (resolved != null)
            {
                MainFile.Logger.Info($"ShoppingCartManager: Resolved shop proxy {item.ItemId} → {resolved.Id.Entry}");
                relicModel = resolved;
            }
        }

        CloseMapBeforeDeckSelection(relicModel);

        var mutableRelic = relicModel.ToMutable();

        await PlayerCmd.LoseGold(item.Price, player, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);
        player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).BoughtRelics.Add(mutableRelic.Id);
        await RelicCmd.Obtain(mutableRelic, player);
        RunManager.Instance.RewardSynchronizer.SyncLocalGoldLost(item.Price);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(mutableRelic);

        MainFile.Logger.Info($"ShoppingCartManager: Purchased relic {item.ItemId} for {item.Price} gold");
        return true;
    }

    private static void CloseMapBeforeDeckSelection(RelicModel relicModel)
    {
        if (relicModel is not (GnarledHammer or SmallDeck) || NMapScreen.Instance?.IsOpen != true)
        {
            return;
        }

        NMapScreen.Instance.Close(animateOut: false);
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
        player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).BoughtPotions.Add(mutablePotion.Id);
        RunManager.Instance.RewardSynchronizer.SyncLocalGoldLost(item.Price);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedPotion(mutablePotion);

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

    /// <summary>
    /// Resolves a shop proxy relic (e.g. RandomForgeShopRelic) into an actual obtainable relic.
    /// Returns the resolved relic, or null if the proxy cannot be resolved.
    /// Registered by cross-mod integrations (HextechRuntimeCompat) to handle special shop entries
    /// that should not be obtained directly.
    /// </summary>
    public static Func<RelicModel, Player, Task<RelicModel?>>? ResolveShopProxyRelic;

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

    private static ModelId? GetEntryModelId(MerchantEntry? entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry => cardEntry.CreationResult?.Card?.Id,
            MerchantRelicEntry relicEntry => relicEntry.Model?.Id,
            MerchantPotionEntry potionEntry => potionEntry.Model?.Id,
            _ => null
        };
    }

    private static bool IsReservationConsumed(string reservationKey)
    {
        if (string.IsNullOrEmpty(reservationKey))
        {
            return false;
        }

        var runState = RunManager.Instance?.State;
        if (runState == null)
        {
            return false;
        }

        foreach (var player in runState.Players)
        {
            if (GetShoppingCartRelic(player)?.IsReservationConsumed(reservationKey) == true)
            {
                return true;
            }
        }

        return false;
    }
}
