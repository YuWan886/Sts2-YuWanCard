using YuWanCard.Core.Abstracts;
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
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class Heartsteel : YuWanRelicModel
{
    private Dictionary<Creature, EnemyDamageTracker>? enemyTrackers;
    private bool _isResolvingColossalAppetite;

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
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        ResetCombatState();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return Task.CompletedTask;

        if (enemyTrackers == null)
        {
            return Task.CompletedTask;
        }

        foreach (var tracker in enemyTrackers.Values)
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
        if (_isResolvingColossalAppetite) return 0m;

        var trackers = GetEnemyTrackers();
        if (!trackers.TryGetValue(target, out EnemyDamageTracker? tracker))
        {
            tracker = new EnemyDamageTracker();
            trackers[target] = tracker;
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
        if (_isResolvingColossalAppetite) return;

        var trackers = GetEnemyTrackers();
        if (!trackers.TryGetValue(target, out EnemyDamageTracker? tracker))
        {
            tracker = new EnemyDamageTracker();
            trackers[target] = tracker;
        }

        if (tracker.HasTriggered) return;

        tracker.DamageThisTurn += result.TotalDamage;
        MainFile.Logger.Debug($"Heartsteel: Dealt {result.TotalDamage} damage to {target.Name}, Total: {tracker.DamageThisTurn}");

        if (tracker.DamageThisTurn >= DynamicVars["Threshold"].BaseValue)
        {
            tracker.HasTriggered = true;
            TriggerCount++;
            InvokeDisplayAmountChanged();
            _hasTriggeredThisDamage = true;
            await TriggerColossalAppetite(target, choiceContext);
        }
    }

    private async Task TriggerColossalAppetite(Creature target, PlayerChoiceContext choiceContext)
    {
        if (Owner == null || Owner.Creature.IsDead) return;

        Flash();
        AudioUtils.Play("res://YuWanCard/sounds/vfx/heart_steel.mp3");

        decimal currentHp = Owner.Creature.CurrentHp;
        decimal bonusDamagePercent = DynamicVars["BonusDamagePercent"].BaseValue;
        decimal bonusDamage = Math.Floor(currentHp * bonusDamagePercent);

        _isResolvingColossalAppetite = true;
        try
        {
            if (bonusDamage > 0 && !target.IsDead)
            {
                await CreatureCmd.Damage(choiceContext, target, bonusDamage, ValueProp.Unpowered, Owner.Creature);
            }

            decimal maxHpGain = DynamicVars["MaxHpGain"].BaseValue;
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);

            MainFile.Logger.Info($"Heartsteel triggered: {bonusDamage} bonus damage, +{maxHpGain} max HP, TriggerCount: {TriggerCount}");
        }
        finally
        {
            _isResolvingColossalAppetite = false;
        }
    }

    private Dictionary<Creature, EnemyDamageTracker> GetEnemyTrackers()
    {
        return enemyTrackers ??= new Dictionary<Creature, EnemyDamageTracker>();
    }

    private void ResetCombatState()
    {
        GetEnemyTrackers().Clear();
        _hasTriggeredThisDamage = false;
        _isResolvingColossalAppetite = false;
    }

    private class EnemyDamageTracker
    {
        public decimal DamageThisTurn { get; set; } = 0m;
        public decimal PendingDamage { get; set; } = 0m;
        public bool HasTriggered { get; set; } = false;
    }
}
