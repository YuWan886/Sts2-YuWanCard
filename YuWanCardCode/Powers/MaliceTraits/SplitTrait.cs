using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SplitTrait : MaliceTraitPowerBase
{
    public override bool ShouldStopCombatFromEnding() => true;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner || Owner.Monster == null || CombatState == null)
        {
            return;
        }

        MonsterModel canonical = ModelDb.GetById<MonsterModel>(Owner.Monster.Id);
        int splitHp = Math.Max(1, Owner.MaxHp / 4);

        for (int i = 0; i < 2; i++)
        {
            Creature clone = await CreatureCmd.Add(canonical.ToMutable(), CombatState, Owner.Side, null);
            await CreatureCmd.SetMaxAndCurrentHp(clone, splitHp);
        }
    }
}
