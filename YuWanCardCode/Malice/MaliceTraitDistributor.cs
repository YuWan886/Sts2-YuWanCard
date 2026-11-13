using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Config;
using YuWanCard.Modifiers;
using YuWanCard.Powers;
using YuWanCard.Powers.MaliceTraits;

namespace YuWanCard.Malice;

public static class MaliceTraitDistributor
{
    public static async Task AssignTraits(Creature creature, int maliceLevel, MaliceModifier modifier)
    {
        if (!MaliceHelper.IsEnemyCombat(creature))
        {
            return;
        }

        bool isMinion = MaliceHelper.IsMinionEnemy(creature);
        if (isMinion && await NormalizeMinionTraitsIfNeeded(creature))
        {
            return;
        }

        if (creature.GetPower<MaliceTraitMarkerPower>() != null)
        {
            return;
        }

        int budget = GetTraitBudget(creature, maliceLevel, isMinion);
        if (budget <= 0)
        {
            return;
        }

        var available = GetAvailableTraits(maliceLevel, isMinion)
            .Where(t => creature.GetPower(ModelDb.GetId(t.PowerType)) == null)
            .Where(t => CanApplyTraitToCreature(creature, t.PowerType))
            .ToList();
        if (available.Count == 0)
        {
            return;
        }

        int actNumber = GetActNumber(creature);
        int traitCount = Math.Min(budget, available.Count);
        for (int i = 0; i < traitCount; i++)
        {
            int index = ChooseWeightedTraitIndex(creature, available, actNumber, isMinion);
            var selected = available[index];
            available.RemoveAt(index);
            await ApplyTrait(creature, selected.PowerType, 1);
        }

        if (traitCount > 0)
        {
            await PowerCmd.Apply<MaliceTraitMarkerPower>(new ThrowingPlayerChoiceContext(),creature, traitCount, creature, null);
        }
    }

    private static async Task<bool> NormalizeMinionTraitsIfNeeded(Creature creature)
    {
        List<MaliceTraitPowerBase> existingTraits = creature.Powers.OfType<MaliceTraitPowerBase>().ToList();
        var marker = creature.GetPower<MaliceTraitMarkerPower>();

        if (existingTraits.Count == 0)
        {
            if (marker != null)
            {
                await PowerCmd.Remove(marker);
            }

            return false;
        }

        if (existingTraits.Count == 1 && marker?.Amount == 1)
        {
            return true;
        }

        foreach (MaliceTraitPowerBase trait in existingTraits)
        {
            await PowerCmd.Remove(trait);
        }

        if (marker != null)
        {
            await PowerCmd.Remove(marker);
        }

        await ApplyTrait(creature, existingTraits[0].GetType(), 1);
        await PowerCmd.Apply<MaliceTraitMarkerPower>(new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
        return true;
    }

    private static int GetTraitBudget(Creature creature, int maliceLevel, bool isMinion)
    {
        if (maliceLevel <= 0)
        {
            return 0;
        }

        if (isMinion)
        {
            return 1;
        }

        var room = creature.CombatState?.RunState?.CurrentRoom;
        bool isBoss = room?.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Boss || creature.IsPrimaryEnemy && room?.IsVictoryRoom == false && creature.Monster?.Title?.GetFormattedText()?.Contains("Boss", StringComparison.OrdinalIgnoreCase) == true;
        bool isElite = room?.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Elite;

        if (isBoss)
        {
            int baseBudget = 1;
            if (maliceLevel >= 7)
            {
                baseBudget++;
            }

            if (maliceLevel >= 10)
            {
                baseBudget++;
            }

            return baseBudget;
        }

        if (isElite)
        {
            int baseBudget = 1;
            if (maliceLevel >= 3)
            {
                baseBudget++;
            }

            if (maliceLevel >= 6)
            {
                baseBudget++;
            }

            return baseBudget;
        }

        int normalBudget = 1;
        if (maliceLevel >= 4)
        {
            normalBudget++;
        }

        return normalBudget;
    }

    private static List<YuWanMaliceTraitDefinition> GetAvailableTraits(int maliceLevel, bool isMinion)
    {
        return YuWanMaliceTraitCatalog.Traits
            .Where(t => IsTraitAvailableAtMalice(t.Rarity, maliceLevel, isMinion))
            .Where(t => YuWanContentAvailability.IsMaliceTraitTypeEnabled(t.PowerType))
            .ToList();
    }

    private static bool IsTraitAvailableAtMalice(MaliceTraitRarity rarity, int maliceLevel, bool isMinion)
    {
        if (isMinion)
        {
            return rarity is MaliceTraitRarity.Common or MaliceTraitRarity.Uncommon;
        }

        return rarity switch
        {
            MaliceTraitRarity.Common => maliceLevel >= 1,
            MaliceTraitRarity.Uncommon => maliceLevel >= 4,
            MaliceTraitRarity.Rare => maliceLevel >= 6,
            MaliceTraitRarity.Legendary => maliceLevel >= 9,
            _ => false
        };
    }

    private static int ChooseWeightedTraitIndex(
        Creature creature,
        List<YuWanMaliceTraitDefinition> available,
        int actNumber,
        bool isMinion)
    {
        List<(int Index, int Weight)> weighted =
            available.Select((trait, index) => (Index: index, Weight: GetTraitWeight(trait.Rarity, actNumber, isMinion)))
                .Where(x => x.Weight > 0)
                .ToList();

        if (weighted.Count == 0)
        {
            return creature.CombatState!.RunState.Rng.CombatCardGeneration.NextInt(available.Count);
        }

        int totalWeight = weighted.Sum(x => x.Weight);
        int roll = creature.CombatState!.RunState.Rng.CombatCardGeneration.NextInt(totalWeight);
        int cumulative = 0;
        foreach (var entry in weighted)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry.Index;
            }
        }

        return weighted[^1].Index;
    }

    private static int GetTraitWeight(MaliceTraitRarity rarity, int actNumber, bool isMinion)
    {
        if (isMinion)
        {
            return rarity switch
            {
                MaliceTraitRarity.Common => Math.Max(20, 80 - (actNumber - 1) * 20),
                MaliceTraitRarity.Uncommon => Math.Min(80, 20 + (actNumber - 1) * 20),
                _ => 0
            };
        }

        return actNumber switch
        {
            <= 1 => rarity switch
            {
                MaliceTraitRarity.Common => 70,
                MaliceTraitRarity.Uncommon => 30,
                _ => 0
            },
            2 => rarity switch
            {
                MaliceTraitRarity.Common => 50,
                MaliceTraitRarity.Uncommon => 35,
                MaliceTraitRarity.Rare => 15,
                _ => 0
            },
            3 => rarity switch
            {
                MaliceTraitRarity.Common => 35,
                MaliceTraitRarity.Uncommon => 35,
                MaliceTraitRarity.Rare => 22,
                MaliceTraitRarity.Legendary => 8,
                _ => 0
            },
            _ => rarity switch
            {
                MaliceTraitRarity.Common => 25,
                MaliceTraitRarity.Uncommon => 35,
                MaliceTraitRarity.Rare => 25,
                MaliceTraitRarity.Legendary => 15,
                _ => 0
            }
        };
    }

    private static int GetActNumber(Creature creature) =>
        (creature.CombatState?.RunState?.CurrentActIndex ?? 0) + 1;

    private static bool CanApplyTraitToCreature(Creature creature, Type powerType)
    {
        if (powerType == typeof(SplitTrait))
        {
            return SplitTrait.CanSplit(creature);
        }

        return true;
    }

    private static async Task ApplyTrait(Creature creature, Type powerType, int amount)
    {
        PowerModel canonical = ModelDb.GetById<PowerModel>(ModelDb.GetId(powerType));
        await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), canonical.ToMutable(), creature, amount, creature, null);
        MainFile.Logger.Info($"MaliceTraitDistributor: applied {powerType.Name} to {creature.ModelId}");
    }
}
