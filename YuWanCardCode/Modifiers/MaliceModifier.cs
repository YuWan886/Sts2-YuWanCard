using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;
using YuWanCard.Malice;
using YuWanCard.Powers;
using YuWanCard.Powers.MaliceTraits;
using YuWanCard.Relics.Malice;
using YuWanCard.Utils;

namespace YuWanCard.Modifiers;

public sealed class MaliceModifier : YuWanModifierModel
{
    public override bool AllowedInCustomRun => false;

    [SavedProperty]
    public int YuWanCard_MaliceLevel { get; set; }

    [SavedProperty]
    public int YuWanCard_MaliceTraitKills { get; set; }

    public int EffectiveMaliceLevel => Math.Clamp(YuWanCard_MaliceLevel, 0, MaliceManager.MaxMaliceLevel);

    public override IEnumerable<IHoverTip> HoverTips => [GetHoverTip(EffectiveMaliceLevel)];

    public override Func<Task>? GenerateNeowOption(EventModel eventModel) => null;

    protected override void AfterRunCreated(RunState runState)
    {
        if (runState.Players.Count > 1)
        {
            MainFile.Logger.Info($"MaliceModifier: initialized in multiplayer at malice {YuWanCard_MaliceLevel}");
            return;
        }

        Player? localPlayer = runState.Players.FirstOrDefault();
        if (localPlayer == null)
        {
            YuWanCard_MaliceLevel = 0;
            return;
        }

        MaliceManager.EnsureConsistency(localPlayer.Character.Id);
        YuWanCard_MaliceLevel = MaliceManager.GetPreferredMalice(localPlayer.Character.Id);
        MainFile.Logger.Info($"MaliceModifier: initialized at malice {YuWanCard_MaliceLevel} for {localPlayer.Character.Id}");
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info($"MaliceModifier: loaded with malice {YuWanCard_MaliceLevel}");
    }

    public override async Task BeforeCombatStartLate()
    {
        if (RunState.CurrentRoom is not CombatRoom combatRoom)
        {
            return;
        }

        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            await ApplyTraitsIfNeeded(creature);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        await ApplyTraitsIfNeeded(creature);
        ApplyHpScaling(creature);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not MinionPower || power.Owner == null || amount <= 0)
        {
            return;
        }

        if (MaliceHelper.IsTraitEnemy(power.Owner))
        {
            await RerollTraitsForLateMinion(power.Owner);
            return;
        }

        await ApplyTraitsIfNeeded(power.Owner);
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (EffectiveMaliceLevel <= 0 || room == null)
            return false;

        bool isElite = room.RoomType == RoomType.Elite;
        bool isBoss = room.RoomType == RoomType.Boss;

        if (!isElite && !isBoss)
            return false;

        float chance = isBoss ? 1.0f : 0.10f;
        if (!DeterministicRandomUtils.RollProbability(player.PlayerRng.Rewards, chance))
            return false;

        List<RelicModel> availableMaliceRelics = GetAvailableMaliceRelics(player);
        RelicModel? relic = DeterministicRandomUtils.PickDeterministicRelic(availableMaliceRelics, player.PlayerRng.Rewards);
        if (relic == null)
        {
            return false;
        }

        rewards.Add(new RelicReward(relic.ToMutable(), player));
        return true;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && dealer.Side == CombatSide.Enemy && EffectiveMaliceLevel >= 5)
        {
            return 1.10m;
        }

        return 1m;
    }

    private void ApplyHpScaling(Creature creature)
    {
        if (EffectiveMaliceLevel <= 0 || creature.Side != CombatSide.Enemy)
        {
            return;
        }

        if (OwnerHasSlothHpScalingDisabled())
        {
            return;
        }

        if (EffectiveMaliceLevel < 2)
        {
            return;
        }

        decimal multiplier = EffectiveMaliceLevel >= 8 ? 1.15m : 1.05m;
        decimal ratio = multiplier; // new/old
        decimal newMaxHp = creature.MaxHp * multiplier;
        decimal newCurrentHp = creature.CurrentHp * ratio;
        creature.SetMaxHpInternal(newMaxHp);
        creature.SetCurrentHpInternal(newCurrentHp);
    }

    private async Task ApplyTraitsIfNeeded(Creature creature)
    {
        if (EffectiveMaliceLevel <= 0 || creature.Side != CombatSide.Enemy)
        {
            return;
        }

        await MaliceTraitDistributor.AssignTraits(creature, EffectiveMaliceLevel, this);
    }

    private async Task RerollTraitsForLateMinion(Creature creature)
    {
        if (!MaliceHelper.IsMinionEnemy(creature))
        {
            return;
        }

        foreach (PowerModel trait in creature.Powers.OfType<MaliceTraitPowerBase>().Cast<PowerModel>().ToList())
        {
            await PowerCmd.Remove(trait);
        }

        await PowerCmd.Remove<MaliceTraitMarkerPower>(creature);
        await ApplyTraitsIfNeeded(creature);
    }

    public override Task AfterDeath(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && MaliceHelper.IsTraitEnemy(creature) && MegaCrit.Sts2.Core.Hooks.Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(creature.CombatState!, creature))
        {
            YuWanCard_MaliceTraitKills++;
        }

        return Task.CompletedTask;
    }

    private bool OwnerHasSlothHpScalingDisabled()
    {
        return RunState.Players.Any(p => p.GetRelic<SlothMalice>() != null);
    }

    public static MaliceModifier? GetMaliceModifier(RunState runState)
    {
        foreach (ModifierModel modifier in runState.Modifiers)
        {
            if (modifier is MaliceModifier maliceModifier)
            {
                return maliceModifier;
            }
        }

        return null;
    }

    public static bool IsMaliceMode(RunState runState)
    {
        return GetMaliceModifier(runState)?.EffectiveMaliceLevel > 0;
    }

    private static List<RelicModel> GetAvailableMaliceRelics(Player player)
    {
        return GetAllMaliceRelics()
            .Where(relic => !player.Relics.Any(existing => existing.Id == relic.Id))
            .ToList();
    }

    private static IEnumerable<RelicModel> GetAllMaliceRelics()
    {
        yield return ModelDb.Relic<EnvyMalice>();
        yield return ModelDb.Relic<GluttonyMalice>();
        yield return ModelDb.Relic<GreedMalice>();
        yield return ModelDb.Relic<LustMalice>();
        yield return ModelDb.Relic<PrideMalice>();
        yield return ModelDb.Relic<SlothMalice>();
        yield return ModelDb.Relic<WrathMalice>();
    }

    public static HoverTip GetHoverTip(int level)
    {
        LocString title;
        if (level > 0)
        {
            title = new LocString("modifiers", "YUWANCARD-MALICE.PORTRAIT_TITLE");
            title.Add("malice", level);
        }
        else
        {
            title = new LocString("modifiers", "YUWANCARD-MALICE.PORTRAIT_TITLE_NO_MALICE");
        }

        LocString description = new LocString("modifiers", "YUWANCARD-MALICE.PORTRAIT_DESCRIPTION");
        description.Add("malices", GetMaliceLines(level));
        return new HoverTip(title, description);
    }

    private static List<string> GetMaliceLines(int level)
    {
        var lines = new List<string>();
        if (level <= 0)
        {
            lines.Add(new LocString("modifiers", "YUWANCARD-MALICE.LEVEL_00.title").GetFormattedText());
            return lines;
        }

        for (int i = 1; i <= level; i++)
        {
            lines.Add(new LocString("modifiers", $"YUWANCARD-MALICE.LEVEL_{i:00}.title").GetFormattedText());
        }

        return lines;
    }
}
