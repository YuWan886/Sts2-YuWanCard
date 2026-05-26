using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Malice;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class GluttonyMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public GluttonyMalice() : base(true)
    {
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner?.Creature == null || Owner.Creature.IsDead || !MaliceHelper.IsTraitEnemy(creature))
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner.Creature, 5, playAnim: true);
    }
}
