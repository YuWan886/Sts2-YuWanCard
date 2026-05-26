using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class LustMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public LustMalice() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
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

        float roll = runState.Rng.UpFront.NextFloat();
        if (roll > 0.15f)
        {
            return false;
        }

        rewards.Add(new RelicReward(player));
        return true;
    }
}
