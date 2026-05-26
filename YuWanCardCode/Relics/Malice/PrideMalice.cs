using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Malice;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class PrideMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public PrideMalice() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner?.Creature == null || !MaliceHelper.IsTraitEnemy(creature))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1, Owner.Creature, null);
    }
}
