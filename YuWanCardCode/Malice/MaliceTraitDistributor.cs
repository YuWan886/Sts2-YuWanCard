using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Modifiers;
using YuWanCard.Powers;
using YuWanCard.Powers.MaliceTraits;
using YuWanCard.Relics.Malice;

namespace YuWanCard.Malice;

public static class MaliceTraitDistributor
{
    private enum TraitRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    private static readonly IReadOnlyList<(Type PowerType, TraitRarity Rarity)> TraitPool =
    [
        (typeof(TankTrait), TraitRarity.Common),
        (typeof(SpeedyTrait), TraitRarity.Common),
        (typeof(RegenTrait), TraitRarity.Common),
        (typeof(FieryTrait), TraitRarity.Common),
        (typeof(WeaknessTrait), TraitRarity.Common),
        (typeof(SlownessTrait), TraitRarity.Common),
        (typeof(GravityTrait), TraitRarity.Common),

        (typeof(PoisonTrait), TraitRarity.Uncommon),
        (typeof(ReflectTrait), TraitRarity.Uncommon),
        (typeof(ProtectionTrait), TraitRarity.Uncommon),
        (typeof(WitherTrait), TraitRarity.Uncommon),
        (typeof(BlindnessTrait), TraitRarity.Uncommon),
        (typeof(ShulkerTrait), TraitRarity.Uncommon),

        (typeof(DrainTrait), TraitRarity.Rare),
        (typeof(GrowthTrait), TraitRarity.Rare),
        (typeof(CounterStrikeTrait), TraitRarity.Rare),
        (typeof(CorrosionTrait), TraitRarity.Rare),
        (typeof(AdaptiveTrait), TraitRarity.Rare),
        (typeof(InvisibleTrait), TraitRarity.Rare),
        (typeof(DispellTrait), TraitRarity.Rare),

        (typeof(UndyingTrait), TraitRarity.Legendary),
        (typeof(DementorTrait), TraitRarity.Legendary),
        (typeof(SplitTrait), TraitRarity.Legendary),
        (typeof(MasterTrait), TraitRarity.Legendary),
        (typeof(KillerAuraTrait), TraitRarity.Legendary),
        (typeof(RagnarokTrait), TraitRarity.Legendary)
    ];

    public static async Task AssignTraits(Creature creature, int maliceLevel, MaliceModifier modifier)
    {
        if (!MaliceHelper.IsEnemyCombat(creature))
        {
            return;
        }

        if (creature.GetPower<MaliceTraitMarkerPower>() != null)
        {
            return;
        }

        int budget = GetTraitBudget(creature, maliceLevel);
        if (budget <= 0)
        {
            return;
        }

        if (HasSuppression(creature, budget))
        {
            return;
        }

        var available = GetAvailableTraits(maliceLevel)
            .Where(t => creature.GetPower(ModelDb.GetId(t.PowerType)) == null)
            .ToList();
        if (available.Count == 0)
        {
            return;
        }

        int traitCount = Math.Min(budget, available.Count);
        int totalTraitCount = traitCount;
        for (int i = 0; i < traitCount; i++)
        {
            int index = creature.CombatState!.RunState.Rng.UpFront.NextInt(available.Count);
            var selected = available[index];
            available.RemoveAt(index);
            await ApplyTrait(creature, selected.PowerType, 1);
        }

        if (traitCount > 0 && creature.CombatState!.RunState.Players.Any(p => p.GetRelic<PrideMalice>() != null))
        {
            totalTraitCount += await MaybeApplyExtraTraitFromPride(creature, maliceLevel, available);
        }

        if (totalTraitCount > 0)
        {
            await PowerCmd.Apply<MaliceTraitMarkerPower>(creature, totalTraitCount, creature, null);
        }
    }

    private static async Task<int> MaybeApplyExtraTraitFromPride(Creature creature, int maliceLevel, List<(Type PowerType, TraitRarity Rarity)> remaining)
    {
        if (remaining.Count == 0)
        {
            return 0;
        }

        float roll = creature.CombatState!.RunState.Rng.UpFront.NextFloat();
        if (roll > 0.5f)
        {
            return 0;
        }

        var available = remaining.Where(t => IsTraitAvailableAtMalice(t.Rarity, maliceLevel)).ToList();
        if (available.Count == 0)
        {
            return 0;
        }

        int index = creature.CombatState.RunState.Rng.UpFront.NextInt(available.Count);
        await ApplyTrait(creature, available[index].PowerType, 1);
        return 1;
    }

    private static int GetTraitBudget(Creature creature, int maliceLevel)
    {
        var room = creature.CombatState?.RunState?.CurrentRoom;
        bool isBoss = room?.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Boss || creature.IsPrimaryEnemy && room?.IsVictoryRoom == false && creature.Monster?.Title?.GetFormattedText()?.Contains("Boss", StringComparison.OrdinalIgnoreCase) == true;
        bool isElite = room?.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Elite;

        if (isBoss)
        {
            int baseBudget = maliceLevel >= 7 ? 1 : 0;
            if (maliceLevel >= 10)
            {
                baseBudget++;
            }

            return baseBudget;
        }

        if (isElite)
        {
            int baseBudget = maliceLevel >= 3 ? 1 : 0;
            if (maliceLevel >= 6)
            {
                baseBudget++;
            }

            return baseBudget;
        }

        int normalBudget = maliceLevel >= 1 ? 1 : 0;
        if (maliceLevel >= 4)
        {
            normalBudget++;
        }

        return normalBudget;
    }

    private static bool HasSuppression(Creature creature, int budget)
    {
        if (budget <= 0)
        {
            return true;
        }

        return creature.CombatState!.RunState.Rng.UpFront.NextFloat() < 0.10f;
    }

    private static List<(Type PowerType, TraitRarity Rarity)> GetAvailableTraits(int maliceLevel)
    {
        return TraitPool.Where(t => IsTraitAvailableAtMalice(t.Rarity, maliceLevel)).ToList();
    }

    private static bool IsTraitAvailableAtMalice(TraitRarity rarity, int maliceLevel) => rarity switch
    {
        TraitRarity.Common => maliceLevel >= 1,
        TraitRarity.Uncommon => maliceLevel >= 4,
        TraitRarity.Rare => maliceLevel >= 6,
        TraitRarity.Legendary => maliceLevel >= 9,
        _ => false
    };

    private static async Task ApplyTrait(Creature creature, Type powerType, int amount)
    {
        PowerModel canonical = ModelDb.GetById<PowerModel>(ModelDb.GetId(powerType));
        await PowerCmd.Apply(canonical.ToMutable(), creature, amount, creature, null);
        MainFile.Logger.Info($"MaliceTraitDistributor: applied {powerType.Name} to {creature.ModelId}");
    }
}
