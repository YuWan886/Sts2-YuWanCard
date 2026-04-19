using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Patches;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class ShoppingCart : YuWanRelicModel
{
    private string _shoppingCartData = string.Empty;
    
    [SavedProperty]
    public string YuWanCard_ShoppingCartData 
    { 
        get => _shoppingCartData;
        set
        {
            if (_shoppingCartData != value)
            {
                _shoppingCartData = value;
                _cartData = null;
            }
        }
    }

    private ShoppingCartData? _cartData;

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
        SaveCartData();
    }

    private void OnCartCleared()
    {
        SaveCartData();
    }

    public void SaveCartData()
    {
        if (_cartData != null)
        {
            var newData = _cartData.Serialize();
            if (_shoppingCartData != newData)
            {
                _shoppingCartData = newData;
                MainFile.Logger.Debug($"ShoppingCart: Saved cart data ({_cartData.Count} items)");
            }
        }
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        MainFile.Logger.Info("ShoppingCart: Relic obtained, initializing cart data");
        GetCartData();
        NTopBar_ShoppingCartPatch.RefreshButtonVisibility();
    }

    public override bool ShowCounter => GetCartData().Count > 0;

    public override int DisplayAmount => GetCartData().Count;
}
