using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class GreedMalice : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public GreedMalice() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom combatRoom)
        {
            return false;
        }

        if (player.RunState is not MegaCrit.Sts2.Core.Runs.RunState runState)
        {
            return false;
        }

        var modifier = Modifiers.MaliceModifier.GetMaliceModifier(runState);
        if (modifier == null || modifier.YuWanCard_MaliceTraitKills <= 0)
        {
            return false;
        }

        rewards.Add(new GoldReward(15 * modifier.YuWanCard_MaliceTraitKills, player));
        return true;
    }
}
