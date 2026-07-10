using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Hextech;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class PigletGuardRune : HextechPigRuneBase
{
    private readonly HashSet<ulong> _guardedPigCombatIdsThisCombat = [];

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(3m),
        new BlockVar(2m, ValueProp.Unpowered)
    ];

    public override async Task BeforeCombatStart()
    {
        _guardedPigCombatIdsThisCombat.Clear();

        if (Owner == null)
        {
            return;
        }

        Creature? pig = PetManager.FindPetByType<Monsters.PigMinion>(Owner.Creature);
        if (pig == null)
        {
            return;
        }

        await ApplyGuardToPig(pig);
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (Owner == null
            || creature.PetOwner?.Creature != Owner.Creature
            || creature.Monster is not Monsters.PigMinion)
        {
            return;
        }

        await ApplyGuardToPig(creature);
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Owner == null
            || creature.PetOwner?.Creature != Owner.Creature
            || creature.Monster is not Monsters.PigMinion
            || !creature.CombatId.HasValue)
        {
            return Task.CompletedTask;
        }

        _guardedPigCombatIdsThisCombat.Remove(creature.CombatId.Value);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player != Owner)
        {
            return;
        }

        Creature? pig = PetManager.FindPetByType<Monsters.PigMinion>(Owner.Creature);
        if (pig == null || pig.IsDead)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }

    private async Task ApplyGuardToPig(Creature pig)
    {
        if (Owner == null || pig.IsDead)
        {
            return;
        }

        if (pig.CombatId.HasValue && !_guardedPigCombatIdsThisCombat.Add(pig.CombatId.Value))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<HextechPigletGuardMinionPower>(new ThrowingPlayerChoiceContext(), pig, 1, Owner.Creature, null);
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), pig, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
    }
}
