using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class Heartsteel : YuWanRelicModel
{
    private Dictionary<ModelId, EnemyDamageTracker> EnemyTrackers { get; set; } = new();

    [SavedProperty]
    private int TriggerCount { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => TriggerCount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("Threshold", 20m),
            new DynamicVar("BonusDamagePercent", 0.1m),
            new DynamicVar("MaxHpGain", 3m)
        ];

    public Heartsteel() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task BeforeCombatStart()
    {
        EnemyTrackers.Clear();
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        EnemyTrackers.Clear();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return Task.CompletedTask;

        foreach (var tracker in EnemyTrackers.Values)
        {
            tracker.DamageThisTurn = 0m;
            tracker.PendingDamage = 0m;
        }

        return Task.CompletedTask;
    }

    private bool _hasTriggeredThisDamage = false;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner?.Creature) return 0m;
        if (target == null || target.Side != CombatSide.Enemy) return 0m;
        if (Owner == null) return 0m;
        if (amount <= 0) return 0m;

        var enemyId = target.ModelId;
        if (!EnemyTrackers.TryGetValue(enemyId, out EnemyDamageTracker? tracker))
        {
            tracker = new EnemyDamageTracker();
            EnemyTrackers[enemyId] = tracker;
        }

        if (tracker.HasTriggered) return 0m;

        tracker.PendingDamage += amount;
        _hasTriggeredThisDamage = false;

        return 0m;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner?.Creature) return;
        if (target == null || target.Side != CombatSide.Enemy) return;
        if (Owner == null) return;
        if (result.TotalDamage <= 0) return;
        if (_hasTriggeredThisDamage) return;

        var enemyId = target.ModelId;
        if (!EnemyTrackers.TryGetValue(enemyId, out EnemyDamageTracker? tracker))
        {
            tracker = new EnemyDamageTracker();
            EnemyTrackers[enemyId] = tracker;
        }

        if (tracker.HasTriggered) return;

        tracker.DamageThisTurn += result.TotalDamage;
        MainFile.Logger.Debug($"Heartsteel: Dealt {result.TotalDamage} damage to {target.Name}, Total: {tracker.DamageThisTurn}");

        if (tracker.DamageThisTurn >= DynamicVars["Threshold"].BaseValue)
        {
            tracker.HasTriggered = true;
            TriggerCount++;
            _hasTriggeredThisDamage = true;
            await TriggerColossalAppetite(target, choiceContext);
        }
    }

    private async Task TriggerColossalAppetite(Creature target, PlayerChoiceContext choiceContext)
    {
        if (Owner == null || Owner.Creature.IsDead) return;

        Flash();

        decimal currentHp = Owner.Creature.CurrentHp;
        decimal bonusDamagePercent = DynamicVars["BonusDamagePercent"].BaseValue;
        decimal bonusDamage = Math.Floor(currentHp * bonusDamagePercent);

        if (bonusDamage > 0 && !target.IsDead)
        {
            await CreatureCmd.Damage(choiceContext, target, bonusDamage, ValueProp.Move, Owner.Creature);
        }

        decimal maxHpGain = DynamicVars["MaxHpGain"].BaseValue;
        await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);

        MainFile.Logger.Info($"Heartsteel triggered: {bonusDamage} bonus damage, +{maxHpGain} max HP, TriggerCount: {TriggerCount}");
    }

    private class EnemyDamageTracker
    {
        public decimal DamageThisTurn { get; set; } = 0m;
        public decimal PendingDamage { get; set; } = 0m;
        public bool HasTriggered { get; set; } = false;
    }
}
