using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class MasterTrait : MaliceTraitPowerBase
{
    private const int SummonInterval = 3;
    private const int MaxSummons = 3;

    private IReadOnlyList<MonsterModel>? _actMonsterPool;
    private int _turnCount;
    private int _summonedCount;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        _turnCount++;

        // Summon every 3 turns, max 3 total
        if (_turnCount % SummonInterval != 0 || _summonedCount >= MaxSummons || combatState.Enemies.Count >= 5)
        {
            return;
        }

        // Build act-specific monster pool on first use (regular encounters only)
        _actMonsterPool ??= BuildActMonsterPool((CombatState)combatState);

        if (_actMonsterPool == null || _actMonsterPool.Count == 0)
        {
            return;
        }

        int index = combatState.RunState.Rng.UpFront.NextInt(_actMonsterPool.Count);
        MonsterModel monster = _actMonsterPool[index].ToMutable();

        // Find a free slot to prevent hitbox overlap (null if encounter has no slots)
        string? slotName = combatState.Encounter?.GetNextSlot(combatState);
        if (string.IsNullOrEmpty(slotName))
            slotName = null;

        Flash();
        Creature summoned = await CreatureCmd.Add(monster, combatState, Owner.Side, slotName);

        // Mark as minion (爪牙)
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), summoned, 1, Owner, null);

        _summonedCount++;
    }

    private static IReadOnlyList<MonsterModel>? BuildActMonsterPool(CombatState combatState)
    {
        var act = combatState.RunState?.Act;
        if (act == null) return null;

        return act.AllEncounters
            .Where(e => e.RoomType == RoomType.Monster)
            .SelectMany(e => e.AllPossibleMonsters)
            .Where(m => m != null)
            .Distinct()
            .ToList();
    }
}
