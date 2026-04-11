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
    [SavedProperty]
    public string YuWanCard_ShoppingCartData { get; set; } = string.Empty;

    private ShoppingCartData? _cartData;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Capacity", 5)];

    protected override string RelicId => "shopping_cart";
    protected override string IconBasePath => $"res://YuWanCard/images/relics/{RelicId}";
    public override string PackedIconPath => $"{IconBasePath}.png";
    protected override string BigIconPath => $"{IconBasePath}.png";
    protected override string PackedIconOutlinePath => $"{IconBasePath}.png";

    public ShoppingCart() : base(true)
    {
    }

    public ShoppingCartData GetCartData()
    {
        if (_cartData == null)
        {
            _cartData = new ShoppingCartData();
            if (!string.IsNullOrEmpty(YuWanCard_ShoppingCartData))
            {
                _cartData.Deserialize(YuWanCard_ShoppingCartData);
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
            YuWanCard_ShoppingCartData = _cartData.Serialize();
            MainFile.Logger.Debug($"ShoppingCart: Saved cart data ({_cartData.Count} items)");
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
