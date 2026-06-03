using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Patches;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class ShoppingCart : YuWanRelicModel
{
    static ShoppingCart()
    {
        SavedPropertyRegistration.RegisterType(typeof(ShoppingCart));
    }

    private string _shoppingCartData = string.Empty;
    private string _consumedReservationData = string.Empty;
    private ShoppingCartData? _cartData;
    private HashSet<string>? _consumedReservationKeys;
    private bool _isDeserializing;

    [SavedProperty]
    public string YuWanCard_ShoppingCartData
    {
        get => _shoppingCartData;
        set
        {
            if (_shoppingCartData != value)
            {
                _shoppingCartData = value;
                if (_cartData != null)
                {
                    _isDeserializing = true;
                    try { _cartData.Deserialize(value); }
                    finally { _isDeserializing = false; }
                }

                RefreshDerivedUi();
            }
        }
    }

    [SavedProperty]
    public string YuWanCard_ShoppingCartConsumedReservations
    {
        get => _consumedReservationData;
        set
        {
            if (_consumedReservationData != value)
            {
                _consumedReservationData = value;
                _consumedReservationKeys = DeserializeReservationKeys(value);
                RefreshDerivedUi();
            }
        }
    }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Capacity", 4)];

    public ShoppingCart() : base(true)
    {
    }

    public ShoppingCartData GetCartData()
    {
        if (_cartData == null)
        {
            _cartData = new ShoppingCartData();
            if (!string.IsNullOrEmpty(_shoppingCartData))
            {
                _cartData.Deserialize(_shoppingCartData);
            }
            _cartData.ItemAdded += OnCartItemChanged;
            _cartData.ItemRemoved += OnCartItemChanged;
            _cartData.CartCleared += OnCartCleared;
        }
        return _cartData;
    }

    private void OnCartItemChanged(ShoppingCartItem _)
    {
        if (!_isDeserializing) SaveCartData();
    }

    private void OnCartCleared()
    {
        if (!_isDeserializing) SaveCartData();
    }

    public void SaveCartData()
    {
        if (_cartData != null)
        {
            var newData = _cartData.Serialize();
            if (_shoppingCartData != newData)
            {
                _isDeserializing = true;
                try
                {
                    YuWanCard_ShoppingCartData = newData;
                }
                finally
                {
                    _isDeserializing = false;
                }
                MainFile.Logger.Debug($"ShoppingCart: Saved cart data ({_cartData.Count} items)");
            }
        }
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        MainFile.Logger.Info("ShoppingCart: Relic obtained, initializing cart data");
        GetCartData();
        RefreshDerivedUi();
    }

    public override bool ShowCounter => GetCartData().Count > 0;

    public override int DisplayAmount => GetCartData().Count;

    internal bool IsReservationConsumed(string? reservationKey)
    {
        return !string.IsNullOrEmpty(reservationKey) && GetConsumedReservationKeys().Contains(reservationKey);
    }

    internal void MarkReservationConsumed(string? reservationKey)
    {
        if (string.IsNullOrEmpty(reservationKey))
        {
            return;
        }

        if (GetConsumedReservationKeys().Add(reservationKey))
        {
            SaveConsumedReservationKeys();
        }
    }

    private void RefreshDerivedUi()
    {
        InvokeDisplayAmountChanged();
        NMerchantSlot_ShoppingCartPatch.RefreshOpenMerchantInventories();

        if (Owner != null && LocalContext.IsMe(Owner))
        {
            NTopBar_ShoppingCartPatch.RefreshButtonVisibility();
        }
    }

    private HashSet<string> GetConsumedReservationKeys()
    {
        return _consumedReservationKeys ??= DeserializeReservationKeys(_consumedReservationData);
    }

    private void SaveConsumedReservationKeys()
    {
        string newData = string.Join(";", GetConsumedReservationKeys().OrderBy(key => key, StringComparer.Ordinal));
        if (_consumedReservationData == newData)
        {
            return;
        }

        _isDeserializing = true;
        try
        {
            YuWanCard_ShoppingCartConsumedReservations = newData;
        }
        finally
        {
            _isDeserializing = false;
        }
    }

    private static HashSet<string> DeserializeReservationKeys(string data)
    {
        return data.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}
