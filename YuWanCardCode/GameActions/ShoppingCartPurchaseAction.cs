using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YuWanCard.Utils;

namespace YuWanCard.GameActions;

public class ShoppingCartPurchaseAction : GameAction
{
    private readonly Player _player;
    private readonly int _itemIndex;

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public ShoppingCartPurchaseAction(Player player, int itemIndex = 0)
    {
        _player = player;
        _itemIndex = itemIndex;
    }

    protected override async Task ExecuteAction()
    {
        var cart = ShoppingCartManager.GetShoppingCartRelic(_player);
        if (cart == null)
            return;

        var data = cart.GetCartData();
        var item = data.GetItem(_itemIndex);
        if (item == null)
            return;

        if (_player.Gold < item.Price)
        {
            MainFile.Logger.Warn($"ShoppingCartPurchaseAction: Not enough gold ({_player.Gold} < {item.Price})");
            return;
        }

        bool success = false;

        switch (item.ItemType)
        {
            case ShoppingCartItemType.Card:
                success = await ShoppingCartManager.PurchaseCard(item, _player);
                break;
            case ShoppingCartItemType.Relic:
                success = await ShoppingCartManager.PurchaseRelic(item, _player);
                break;
            case ShoppingCartItemType.Potion:
                success = await ShoppingCartManager.PurchasePotion(item, _player);
                break;
        }

        if (success)
        {
            data.RemoveAt(_itemIndex);
            cart.SaveCartData();
            MainFile.Logger.Info($"ShoppingCartPurchaseAction: Purchased {item.ItemId} for {item.Price} gold");
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetShoppingCartPurchaseAction(_itemIndex);
    }

    public override string ToString()
    {
        return $"ShoppingCartPurchaseAction player={_player.NetId} index={_itemIndex}";
    }
}

public struct NetShoppingCartPurchaseAction : INetAction, IPacketSerializable
{
    private int _itemIndex;

    public NetShoppingCartPurchaseAction(int itemIndex)
    {
        _itemIndex = itemIndex;
    }

    public GameAction ToGameAction(Player owner)
    {
        return new ShoppingCartPurchaseAction(owner, _itemIndex);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(_itemIndex);
    }

    public void Deserialize(PacketReader reader)
    {
        _itemIndex = reader.ReadInt();
    }
}
