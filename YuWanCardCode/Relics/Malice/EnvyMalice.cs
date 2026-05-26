using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class EnvyMalice : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public EnvyMalice() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
        {
            return false;
        }

        if (player.RunState is not MegaCrit.Sts2.Core.Runs.RunState)
        {
            return false;
        }

        var modifier = Modifiers.MaliceModifier.GetMaliceModifier((MegaCrit.Sts2.Core.Runs.RunState)player.RunState);
        if (modifier == null || modifier.YuWanCard_MaliceTraitKills <= 0)
        {
            return false;
        }

        rewards.Add(new CardReward(MegaCrit.Sts2.Core.Runs.CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
        return true;
    }
}
