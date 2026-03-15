using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Modifiers;

public class EndlessModifier : ModifierModel
{
    public const string ModifierId = "YUWANCARD-ENDLESS";

    private const float BaseHpMultiplierPerLoop = 0.20f;
    private const float BossHpMultiplierBonus = 0.10f;
    private const int BaseStrengthPerLoop = 1;
    private const int BossExtraStrengthPerLoop = 1;
    private const float HpGrowthExponent = 1.1f;

    [SavedProperty]
    public int EndlessLoopCount { get; set; } = 0;

    [SavedProperty]
    public int TotalActsCleared { get; set; } = 0;

    [SavedProperty]
    public bool HasStarted { get; set; } = false;

    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");

    public override string IconPath => "res://YuWanCard/images/modifiers/endless.png";

    public int EffectiveLoopCount => Math.Max(0, EndlessLoopCount);

    private float CalculateHpMultiplier(bool isBoss)
    {
        if (EffectiveLoopCount <= 0) return 1f;
        
        float baseMultiplier = 1f + (BaseHpMultiplierPerLoop * (float)Math.Pow(EffectiveLoopCount, HpGrowthExponent));
        
        if (isBoss)
        {
            baseMultiplier += BossHpMultiplierBonus * EffectiveLoopCount;
        }
        
        return baseMultiplier;
    }

    private int CalculateStrengthBonus(bool isBoss)
    {
        if (EffectiveLoopCount <= 0) return 0;
        
        int bonus = BaseStrengthPerLoop * EffectiveLoopCount;
        
        if (isBoss)
        {
            bonus += BossExtraStrengthPerLoop * (EffectiveLoopCount / 2);
        }
        
        return bonus;
    }

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        if (HasStarted)
        {
            return null;
        }
        return () => ActivateEndlessMode(eventModel.Owner!, eventModel.Rng);
    }

    private async Task ActivateEndlessMode(Player player, Rng rng)
    {
        MainFile.Logger.Info("Endless mode activated!");

        HasStarted = true;

        foreach (Player p in player.RunState.Players)
        {
            await CreatureCmd.GainMaxHp(p.Creature, 10m);
        }
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
        {
            return;
        }

        if (EffectiveLoopCount <= 0)
        {
            return;
        }

        bool isBoss = combatRoom.RoomType == RoomType.Boss;
        
        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            await ApplyDifficultyScaling(creature, isBoss);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy)
        {
            return;
        }

        if (EffectiveLoopCount <= 0)
        {
            return;
        }

        var combatRoom = creature.CombatState?.RunState?.CurrentRoom as CombatRoom;
        bool isBoss = combatRoom?.RoomType == RoomType.Boss;

        await ApplyDifficultyScaling(creature, isBoss);
    }

    private async Task ApplyDifficultyScaling(Creature creature, bool isBoss)
    {
        float hpMultiplier = CalculateHpMultiplier(isBoss);
        int strengthBonus = CalculateStrengthBonus(isBoss);

        int newMaxHp = (int)(creature.MaxHp * hpMultiplier);
        await CreatureCmd.SetMaxHp(creature, newMaxHp);
        await CreatureCmd.Heal(creature, newMaxHp - creature.CurrentHp, playAnim: false);

        if (strengthBonus > 0)
        {
            await PowerCmd.Apply<StrengthPower>(creature, (decimal)strengthBonus, null, null);
        }

        MainFile.Logger.Info($"Applied endless difficulty to {creature.ModelId} (Boss: {isBoss}): HP x{hpMultiplier:F2}, Strength +{strengthBonus}");
    }

    public override bool ShouldAllowAncient(Player player, AncientEventModel ancient)
    {
        if (ancient is Neow && EffectiveLoopCount > 0)
        {
            return false;
        }
        return true;
    }

    public override void AfterRunCreated(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier initialized. Loop: {EndlessLoopCount}, TotalActs: {TotalActsCleared}");
    }

    public override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier loaded. Loop: {EndlessLoopCount}, TotalActs: {TotalActsCleared}");
    }

    public void IncrementLoopCount()
    {
        EndlessLoopCount++;
        TotalActsCleared++;
        MainFile.Logger.Info($"Endless loop incremented. Now at loop {EndlessLoopCount}, total acts: {TotalActsCleared}");
    }

    public void IncrementActCount()
    {
        TotalActsCleared++;
    }

    public static EndlessModifier? GetEndlessModifier(RunState runState)
    {
        foreach (var modifier in runState.Modifiers)
        {
            if (modifier is EndlessModifier endlessModifier)
            {
                return endlessModifier;
            }
        }
        return null;
    }

    public static bool IsEndlessMode(RunState runState)
    {
        return GetEndlessModifier(runState) != null;
    }
}
