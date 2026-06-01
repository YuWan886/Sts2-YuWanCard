using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Hextech;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class PigBreederRune : HextechPigRuneBase
{
    private readonly HashSet<ulong> _processedSummonsThisCombat = [];

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ExtraUpgrade", 1m)];

    public override Task BeforeCombatStart()
    {
        _processedSummonsThisCombat.Clear();
        return Task.CompletedTask;
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (Owner == null
            || creature.PetOwner?.Creature != Owner.Creature
            || creature.Monster is not PigMinion
            || !creature.CombatId.HasValue
            || !_processedSummonsThisCombat.Add(creature.CombatId.Value))
        {
            return;
        }

        Flash();
        await PetManager.UpgradePigMinion(creature, DynamicVars["ExtraUpgrade"].IntValue, Owner.Creature);
    }

}
