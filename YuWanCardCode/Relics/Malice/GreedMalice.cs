using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Malice;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(MaliceRelicPool))]
public sealed class GreedMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    private int _combatTraitKills;

    public GreedMalice() : base(true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _combatTraitKills = 0;
        return Task.CompletedTask;
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && MaliceHelper.IsTraitEnemy(creature))
        {
            _combatTraitKills++;
        }
        return Task.CompletedTask;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
        {
            return false;
        }

        if (_combatTraitKills <= 0)
        {
            return false;
        }

        rewards.Add(new GoldReward(40 * _combatTraitKills, player));
        return true;
    }
}
