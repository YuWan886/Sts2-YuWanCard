using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Utils;

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

        if (!MegaCrit.Sts2.Core.Hooks.Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(CombatState, creature))
        {
            return;
        }

        MonsterModel canonical = ModelDb.GetById<MonsterModel>(Owner.Monster.Id);
        int splitHp = Math.Max(1, Owner.MaxHp / 4);
        Vector2 splitCenter = EnemySpawnPositionUtils.GetCreatureCenterPosition(Owner);
        List<Creature> slotlessClones = [];

        for (int i = 0; i < 2; i++)
        {
            string? slotName = EnemySpawnPositionUtils.GetNextEnemySlot(CombatState);
            Creature clone = await CreatureCmd.Add(canonical.ToMutable(), CombatState, Owner.Side, slotName);
            await CreatureCmd.SetMaxAndCurrentHp(clone, splitHp);

            if (slotName == null)
            {
                slotlessClones.Add(clone);
            }
        }

        if (slotlessClones.Count >= 2)
        {
            EnemySpawnPositionUtils.SpreadSummonsAroundPosition(slotlessClones, splitCenter);
        }
        else if (slotlessClones.Count == 1)
        {
            EnemySpawnPositionUtils.PositionSummonWithoutSlot(slotlessClones[0], Owner);
        }
    }
}
