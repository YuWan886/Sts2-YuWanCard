using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class WhiteScarf : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public WhiteScarf() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }
        if (room == null)
        {
            return false;
        }

        if (room.RoomType == RoomType.Monster || room.RoomType == RoomType.Boss || room.RoomType == RoomType.Elite)
        {
            var colorlessPool = ModelDb.CardPool<ColorlessCardPool>();
            var colorlessCards = colorlessPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
            var options = CardCreationOptions.ForRoom(player, room.RoomType).WithCustomPool(colorlessCards);
            rewards.Add(new CardReward(options, 3, player));
        }

        return true;
    }
}
