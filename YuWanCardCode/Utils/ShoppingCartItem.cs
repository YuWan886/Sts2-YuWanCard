using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Utils;

public enum ShoppingCartItemType
{
    Card,
    Relic,
    Potion
}

public class ShoppingCartItem
{
    public ShoppingCartItemType ItemType { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Price { get; set; }
    public bool IsOnSale { get; set; }
    public DateTime AddedTime { get; set; }
    
    public ModelId? ModelId { get; set; }
    public string ReservationKey { get; set; } = string.Empty;

    public ShoppingCartItem() { }

    public ShoppingCartItem(MerchantCardEntry cardEntry)
    {
        ItemType = ShoppingCartItemType.Card;
        ItemId = cardEntry.CreationResult?.Card?.Id?.Entry ?? string.Empty;
        Price = cardEntry.Cost;
        IsOnSale = cardEntry.IsOnSale;
        AddedTime = DateTime.UtcNow;
        ModelId = cardEntry.CreationResult?.Card?.Id;
        ReservationKey = CreateReservationKey(cardEntry);
    }

    public ShoppingCartItem(MerchantRelicEntry relicEntry)
    {
        ItemType = ShoppingCartItemType.Relic;
        ItemId = relicEntry.Model?.Id?.Entry ?? string.Empty;
        Price = relicEntry.Cost;
        IsOnSale = false;
        AddedTime = DateTime.UtcNow;
        ModelId = relicEntry.Model?.Id;
        ReservationKey = CreateReservationKey(relicEntry);
    }

    public ShoppingCartItem(MerchantPotionEntry potionEntry)
    {
        ItemType = ShoppingCartItemType.Potion;
        ItemId = potionEntry.Model?.Id?.Entry ?? string.Empty;
        Price = potionEntry.Cost;
        IsOnSale = false;
        AddedTime = DateTime.UtcNow;
        ModelId = potionEntry.Model?.Id;
        ReservationKey = CreateReservationKey(potionEntry);
    }

    public string Serialize()
    {
        return $"{(int)ItemType}|{ItemId}|{Price}|{IsOnSale}|{AddedTime.Ticks}|{ModelId?.Category ?? ""}|{ModelId?.Entry ?? ""}|{ReservationKey}";
    }

    public static ShoppingCartItem? Deserialize(string data)
    {
        if (string.IsNullOrEmpty(data))
            return null;

        var parts = data.Split('|');
        if (parts.Length < 5)
            return null;

        try
        {
            var item = new ShoppingCartItem
            {
                ItemType = (ShoppingCartItemType)int.Parse(parts[0]),
                ItemId = parts[1],
                Price = int.Parse(parts[2]),
                IsOnSale = bool.Parse(parts[3]),
                AddedTime = new DateTime(long.Parse(parts[4]))
            };

            if (parts.Length >= 7 && !string.IsNullOrEmpty(parts[5]) && !string.IsNullOrEmpty(parts[6]))
            {
                item.ModelId = new ModelId(parts[5], parts[6]);
            }

            if (parts.Length >= 8)
            {
                item.ReservationKey = parts[7];
            }

            return item;
        }
        catch
        {
            return null;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is ShoppingCartItem other)
        {
            return ItemType == other.ItemType && ItemId == other.ItemId;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ItemType, ItemId);
    }

    public static string CreateReservationKey(MerchantEntry entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry => CreateReservationKey(
                ShoppingCartItemType.Card,
                cardEntry.CreationResult?.Card?.Id,
                cardEntry.Cost,
                cardEntry.IsOnSale),
            MerchantRelicEntry relicEntry => CreateReservationKey(
                ShoppingCartItemType.Relic,
                relicEntry.Model?.Id,
                relicEntry.Cost,
                false),
            MerchantPotionEntry potionEntry => CreateReservationKey(
                ShoppingCartItemType.Potion,
                potionEntry.Model?.Id,
                potionEntry.Cost,
                false),
            _ => string.Empty
        };
    }

    private static string CreateReservationKey(
        ShoppingCartItemType itemType,
        ModelId? modelId,
        int price,
        bool isOnSale)
    {
        RunLocation? location = RunManager.Instance?.RunLocationTargetedBuffer?.CurrentLocation;
        int actIndex = location?.mapLocation.actIndex ?? -1;
        int col = location?.mapLocation.coord?.col ?? -1;
        int row = location?.mapLocation.coord?.row ?? -1;
        int roomId = location?.roomId ?? -1;

        return string.Join(
            "~",
            actIndex,
            col,
            row,
            roomId,
            (int)itemType,
            modelId?.Category ?? string.Empty,
            modelId?.Entry ?? string.Empty,
            price,
            isOnSale ? 1 : 0);
    }
}
