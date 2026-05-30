namespace YuWanCard.Utils;

public class ShoppingCartData
{
    public const int MaxCapacity = 5;

    private int _capacity = MaxCapacity;

    public int Capacity
    {
        get => _capacity;
        set => _capacity = Math.Max(0, value);
    }

    /// <summary>
    /// Multiplier applied to item prices when added to the cart.
    /// 1.0 = no discount, 0.8 = 20% off.
    /// </summary>
    public double DiscountMultiplier { get; set; } = 1.0;

    private List<ShoppingCartItem> _items = new();

    public IReadOnlyList<ShoppingCartItem> Items => _items.AsReadOnly();

    public int Count => _items.Count;

    public bool IsFull => _items.Count >= Capacity;

    public bool IsEmpty => _items.Count == 0;

    public int TotalPrice => _items.Sum(item => item.Price);

    public event Action<ShoppingCartItem>? ItemAdded;
    public event Action<ShoppingCartItem>? ItemRemoved;
    public event Action? CartCleared;

    public bool AddItem(ShoppingCartItem item)
    {
        if (IsFull)
        {
            MainFile.Logger.Warn($"ShoppingCart: Cannot add item, cart is full ({Capacity} items max)");
            return false;
        }

        if (_items.Contains(item))
        {
            MainFile.Logger.Warn($"ShoppingCart: Item already in cart: {item.ItemId}");
            return false;
        }

        if (DiscountMultiplier < 1.0)
        {
            var originalPrice = item.Price;
            item.Price = (int)Math.Floor(item.Price * DiscountMultiplier);
            MainFile.Logger.Info($"ShoppingCart: Applied {((1.0 - DiscountMultiplier) * 100):0}% discount: {originalPrice} → {item.Price}");
        }

        _items.Add(item);
        ItemAdded?.Invoke(item);
        MainFile.Logger.Info($"ShoppingCart: Added item {item.ItemId} (Type: {item.ItemType}, Price: {item.Price})");
        return true;
    }

    public bool RemoveItem(ShoppingCartItem item)
    {
        var removed = _items.Remove(item);
        if (removed)
        {
            ItemRemoved?.Invoke(item);
            MainFile.Logger.Info($"ShoppingCart: Removed item {item.ItemId}");
        }
        return removed;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count)
            return false;

        var item = _items[index];
        _items.RemoveAt(index);
        ItemRemoved?.Invoke(item);
        MainFile.Logger.Info($"ShoppingCart: Removed item at index {index}: {item.ItemId}");
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        CartCleared?.Invoke();
        MainFile.Logger.Info("ShoppingCart: Cart cleared");
    }

    public string Serialize()
    {
        if (_items.Count == 0)
            return string.Empty;

        return string.Join(";", _items.Select(item => item.Serialize()));
    }

    public void Deserialize(string data)
    {
        CartCleared?.Invoke();
        _items.Clear();

        if (string.IsNullOrEmpty(data))
            return;

        var itemStrings = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var itemString in itemStrings)
        {
            if (IsFull)
            {
                MainFile.Logger.Warn($"ShoppingCart: Deserialization stopped at capacity limit ({Capacity} items max)");
                break;
            }

            var item = ShoppingCartItem.Deserialize(itemString);
            if (item != null)
            {
                _items.Add(item);
            }
        }

        MainFile.Logger.Info($"ShoppingCart: Deserialized {_items.Count} items");
    }

    public bool HasItem(ShoppingCartItem item)
    {
        return _items.Contains(item);
    }

    public ShoppingCartItem? GetItem(int index)
    {
        if (index < 0 || index >= _items.Count)
            return null;
        return _items[index];
    }
}
