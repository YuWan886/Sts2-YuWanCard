using Godot;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Monsters;

public sealed class Ignis : YuWanMonsterModel
{
    private const decimal PhaseTwoHpRatio = 0.67m;
    private const decimal PhaseThreeHpRatio = 0.33m;

    private const int ShieldDamageCap = 20;

    private const int PhaseOneFireballDamage = 6;
    private const int PhaseTwoFireballDamage = 7;
    private const int FireballHits = 5;

    private const int GreatswordQuakeHits = 3;

    private const int SoulflameSlashHits = 3;

    private const int RageComboHits = 8;

    private const string AttackSfxPath = "event:/sfx/characters/attack_fire";
    private const string CastSfxPath = "event:/sfx/enemy/enemy_attacks/queen/queen_cast";
    private const string ShieldBreakSfxPath = "event:/sfx/block_break";
    private const string ShieldAwakeningSfxPath = "event:/sfx/enemy/enemy_attacks/magi_knight/magi_knight_cast_shield";
    private const string PhaseOneVisualPath = "res://YuWanCard/images/monsters/ignis/ignis_1.png";
    private const string PhaseTwoVisualPath = "res://YuWanCard/images/monsters/ignis/ignis_2.png";
    private const string PhaseThreeVisualPath = "res://YuWanCard/images/monsters/ignis/ignis_3.png";
    private static readonly Color FlameTint = Color.FromHtml("#ff8b57");
    private static readonly Color BlueFlameTint = Color.FromHtml("#4fd3ff");
    private static readonly Color SoulflameTint = Color.FromHtml("#8af2ff");

    private int _phase = 1;
    private bool _pendingSoulflameOpener;
    private bool _phaseTurnInterruptionQueued;
    private MoveState? _phaseTwoTransitionMove;
    private MoveState? _phaseThreeTransitionMove;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 470, 450);

    public override int MaxInitialHp => MinInitialHp;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override bool CanChangeScale => false;

    public override string? CustomAttackSfx => AttackSfxPath;

    public override string? CustomCastSfx => CastSfxPath;

    public override string? CustomDeathSfx => "event:/sfx/enemy/enemy_attacks/hunter_killer/hunter_killer_die";

    private bool ShouldStartPhaseTwo =>
        Creature != null &&
        Creature.IsAlive &&
        _phase == 1 &&
        Creature.CurrentHp <= Creature.MaxHp * PhaseTwoHpRatio;

    private bool ShouldStartPhaseThree =>
        Creature != null &&
        Creature.IsAlive &&
        _phase == 2 &&
        Creature.CurrentHp <= Creature.MaxHp * PhaseThreeHpRatio;

    private bool IsSoulflamePhase => _phase == 2;

    private bool IsBlueFlamePhase => _phase == 3;

    private bool ShouldUseSoulflameOpener => IsSoulflamePhase && _pendingSoulflameOpener;

    private Color CurrentPhaseTint => _phase switch
    {
        2 => SoulflameTint,
        3 => BlueFlameTint,
        _ => FlameTint
    };

    private int FlameSlashDamage => IsSoulflamePhase
        ? AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17)
        : AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

    private int BlazingChargeDamage => _phase switch
    {
        1 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14),
        2 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16),
        _ => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16)
    };

    private int ShieldSmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private int ShieldQuakeDamage => IsBlueFlamePhase
        ? AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 28)
        : AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 22);

    private int CounterBlock => IsBlueFlamePhase
        ? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 24, 20)
        : AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 20, 16);

    private int CounterDamage => IsBlueFlamePhase
        ? AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10)
        : AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    private int SoulflameTransitionBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 14);

    private int SoulflameTransitionStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int BlueFlameTransitionBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 30, 24);

    private int BlueFlameTransitionStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int GreatswordQuakeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

    private int SoulflameSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    private int RageComboDamage => 4;

    private int FlameUppercutDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private int BladeBurnCount => _phase >= 2 ? 2 : 1;

    private int FireballBurnCount => _phase >= 2 ? 2 : 1;

    private int StunDazedCount => _phase >= 2 ? 2 : 1;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = [];

        MoveState flameSlash = new("FLAME_SLASH_MOVE", FlameSlashMove,
            new SingleAttackIntent(() => FlameSlashDamage),
            new HealIntent(),
            new StatusIntent(1));
        MoveState blazingCharge = new("BLAZING_CHARGE_MOVE", BlazingChargeMove,
            new SingleAttackIntent(() => BlazingChargeDamage),
            new HealIntent(),
            new StatusIntent(1));
        MoveState shieldSmash = new("SHIELD_SMASH_MOVE", ShieldSmashMove,
            new SingleAttackIntent(ShieldSmashDamage),
            new DebuffIntent());
        MoveState shieldQuake = new("SHIELD_QUAKE_MOVE", ShieldQuakeMove,
            new SingleAttackIntent(() => ShieldQuakeDamage),
            new StatusIntent(2));
        MoveState blazingFireballs = new("BLAZING_FIREBALLS_MOVE", BlazingFireballsMove,
            new MultiAttackIntent(PhaseOneFireballDamage, FireballHits),
            new HealIntent(),
            new StatusIntent(1));
        MoveState counterStance = new("COUNTER_STANCE_MOVE", CounterStanceMove,
            new DefendIntent(),
            new BuffIntent());
        MoveState blueFlameAwakening = new("BLUE_FLAME_AWAKENING_MOVE", BlueFlameAwakeningMove,
            new BuffIntent(),
            new DefendIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState greatswordQuake = new("GREATSWORD_QUAKE_MOVE", GreatswordQuakeMove,
            new MultiAttackIntent(GreatswordQuakeDamage, GreatswordQuakeHits),
            new StatusIntent(1));
        MoveState shieldBreak = new("SHIELD_BREAK_MOVE", ShieldBreakMove,
            new BuffIntent(),
            new DefendIntent(),
            new StatusIntent(1))
        {
            MustPerformOnceBeforeTransitioning = true
        };
        _phaseTwoTransitionMove = shieldBreak;
        _phaseThreeTransitionMove = blueFlameAwakening;
        MoveState soulflameSlash = new("SOULFLAME_SLASH_MOVE", SoulflameSlashMove,
            new MultiAttackIntent(SoulflameSlashDamage, SoulflameSlashHits),
            new StatusIntent(2));
        MoveState rageCombo = new("RAGE_COMBO_MOVE", RageComboMove,
            new MultiAttackIntent(RageComboDamage, RageComboHits),
            new HealIntent(),
            new StatusIntent(1));
        MoveState flameUppercut = new("FLAME_UPPERCUT_MOVE", FlameUppercutMove,
            new SingleAttackIntent(FlameUppercutDamage),
            new DebuffIntent(),
            new StatusIntent(1));

        RandomBranchState phaseOneBranch = new("PHASE_ONE_BRANCH");
        phaseOneBranch.AddBranch(flameSlash, MoveRepeatType.CannotRepeat, 2f);
        phaseOneBranch.AddBranch(blazingCharge, MoveRepeatType.CannotRepeat, 1.75f);
        phaseOneBranch.AddBranch(shieldSmash, 1, 1.25f);
        phaseOneBranch.AddBranch(shieldQuake, 1, 1.25f);
        phaseOneBranch.AddBranch(blazingFireballs, 1, 1f);
        phaseOneBranch.AddBranch(counterStance, 1, 0.9f);

        RandomBranchState phaseTwoBranch = new("PHASE_TWO_BRANCH");
        phaseTwoBranch.AddBranch(rageCombo, MoveRepeatType.CannotRepeat, 2f);
        phaseTwoBranch.AddBranch(soulflameSlash, 1, 1.25f);
        phaseTwoBranch.AddBranch(flameUppercut, MoveRepeatType.CannotRepeat, 1.2f);
        phaseTwoBranch.AddBranch(blazingCharge, MoveRepeatType.CannotRepeat, 1f);
        phaseTwoBranch.AddBranch(flameSlash, MoveRepeatType.CannotRepeat, 1f);

        RandomBranchState phaseThreeBranch = new("PHASE_THREE_BRANCH");
        phaseThreeBranch.AddBranch(flameSlash, MoveRepeatType.CannotRepeat, 1.5f);
        phaseThreeBranch.AddBranch(blazingCharge, MoveRepeatType.CannotRepeat, 1.5f);
        phaseThreeBranch.AddBranch(shieldSmash, 1, 1f);
        phaseThreeBranch.AddBranch(greatswordQuake, 1, 1.4f);
        phaseThreeBranch.AddBranch(blazingFireballs, 1, 1.5f);
        phaseThreeBranch.AddBranch(counterStance, 1, 1f);

        ConditionalBranchState controlBranch = new("CONTROL_BRANCH");
        controlBranch.AddState(shieldBreak, () => ShouldStartPhaseTwo);
        controlBranch.AddState(soulflameSlash, () => ShouldUseSoulflameOpener);
        controlBranch.AddState(blueFlameAwakening, () => ShouldStartPhaseThree);
        controlBranch.AddState(phaseThreeBranch, () => _phase == 3);
        controlBranch.AddState(phaseTwoBranch, () => _phase == 2);
        controlBranch.AddState(phaseOneBranch, () => _phase == 1);

        flameSlash.FollowUpState = controlBranch;
        blazingCharge.FollowUpState = controlBranch;
        shieldSmash.FollowUpState = controlBranch;
        shieldQuake.FollowUpState = controlBranch;
        blazingFireballs.FollowUpState = controlBranch;
        counterStance.FollowUpState = controlBranch;
        blueFlameAwakening.FollowUpState = controlBranch;
        greatswordQuake.FollowUpState = controlBranch;
        shieldBreak.FollowUpState = controlBranch;
        soulflameSlash.FollowUpState = controlBranch;
        rageCombo.FollowUpState = controlBranch;
        flameUppercut.FollowUpState = controlBranch;

        states.Add(controlBranch);
        states.Add(phaseOneBranch);
        states.Add(phaseTwoBranch);
        states.Add(phaseThreeBranch);
        states.Add(flameSlash);
        states.Add(blazingCharge);
        states.Add(shieldSmash);
        states.Add(shieldQuake);
        states.Add(blazingFireballs);
        states.Add(counterStance);
        states.Add(blueFlameAwakening);
        states.Add(greatswordQuake);
        states.Add(shieldBreak);
        states.Add(soulflameSlash);
        states.Add(rageCombo);
        states.Add(flameUppercut);

        return new MonsterMoveStateMachine(states, flameSlash);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _phaseTurnInterruptionQueued = false;
        await PowerCmd.Apply<IgnisShieldPower>(Creature, ShieldDamageCap, Creature, null);
        UpdatePhaseVisual(1);
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Creature || delta >= 0 || Creature.IsDead || CombatState == null)
        {
            return Task.CompletedTask;
        }

        if (CombatState.CurrentSide != CombatSide.Player || _phaseTurnInterruptionQueued)
        {
            return Task.CompletedTask;
        }

        MoveState? transitionMove = null;
        string? phaseLabel = null;
        if (_phase == 1 && Creature.CurrentHp <= Creature.MaxHp * PhaseTwoHpRatio)
        {
            transitionMove = _phaseTwoTransitionMove;
            phaseLabel = "phase 2";
        }
        else if (_phase == 2 && Creature.CurrentHp <= Creature.MaxHp * PhaseThreeHpRatio)
        {
            transitionMove = _phaseThreeTransitionMove;
            phaseLabel = "phase 3";
        }

        if (transitionMove == null)
        {
            return Task.CompletedTask;
        }

        _phaseTurnInterruptionQueued = true;
        SetMoveImmediate(transitionMove, forceTransition: true);
        MainFile.Logger.Info($"Ignis: HP threshold reached, forcing player turn end and entering {phaseLabel}.");

        foreach (Player player in CombatState.Players)
        {
            PlayerCmd.EndTurn(player, canBackOut: false);
        }

        return Task.CompletedTask;
    }

    private async Task FlameSlashMove(IReadOnlyList<Creature> targets)
    {
        var slashTint = CurrentPhaseTint;
        PlaySlashPrep(targets, slashTint);
        await DamageCmd.Attack(FlameSlashDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.18f)
            .WithAttackerFx(null, CustomAttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithHitVfxNode(target => CreateSlashImpact(target, slashTint, 65f))
            .Execute(null);
        await AddBurns(targets, BladeBurnCount);
        await HealSelf(4);
    }

    private async Task BlazingChargeMove(IReadOnlyList<Creature> targets)
    {
        Color chargeTint = CurrentPhaseTint;
        PlayChargePrep(chargeTint);
        await DamageCmd.Attack(BlazingChargeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.22f)
            .WithAttackerFx(null, CustomAttackSfx)
            .AfterAttackerAnim(() =>
            {
                PlayHeavyStrikeBurst(chargeTint, 1.1f);
                return Task.CompletedTask;
            })
            .WithHitFx(VfxCmd.giantHorizontalSlashPath)
            .WithHitVfxNode(target => CreateSlashImpact(target, chargeTint, 110f))
            .Execute(null);
        await AddBurns(targets, BladeBurnCount);
        await HealSelf(4);
    }

    private async Task ShieldSmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ShieldSmashDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, CustomAttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitVfxNode(target => NLineBurstVfx.Create(target))
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(targets, 1m, Creature, null);
        await AddDazed(targets, 1);
    }

    private async Task ShieldQuakeMove(IReadOnlyList<Creature> targets)
    {
        Color quakeTint = CurrentPhaseTint;
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.45f);
        PlayHeavyStrikeBurst(quakeTint, 1.2f);
        PlayTargetFlamePuffs(targets);
        await DamageCmd.Attack(ShieldQuakeDamage)
            .FromMonster(this)
            .WithHitFx(VfxCmd.heavyBluntPath)
            .WithHitVfxNode(target => NLineBurstVfx.Create(target))
            .Execute(null);
        await AddDazed(targets, StunDazedCount);
    }

    private async Task BlazingFireballsMove(IReadOnlyList<Creature> targets)
    {
        int fireballDamage = _phase >= 2 ? PhaseTwoFireballDamage : PhaseOneFireballDamage;
        Color fireballTint = CurrentPhaseTint;
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.4f);
        PlaySelfFlames(fireballTint, 1.05f, 0.95f);
        PlayTargetFlamePuffs(targets);
        await DamageCmd.Attack(fireballDamage)
            .WithHitCount(FireballHits)
            .OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await AddBurns(targets, FireballBurnCount);
        await HealSelf(5);
    }

    private async Task CounterStanceMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.45f);
        await CreatureCmd.GainBlock(Creature, CounterBlock, ValueProp.Move, null);
        await PowerCmd.Apply<IgnisCounterPower>(Creature, CounterDamage, Creature, null);
    }

    private async Task BlueFlameAwakeningMove(IReadOnlyList<Creature> targets)
    {
        _phaseTurnInterruptionQueued = false;
        _phase = 3;
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.6f);
        await PlayPhaseTransitionVfx(_phase, BlueFlameTint, 1.3f, false);
        await RemoveSelfDebuffs();
        await PowerCmd.Apply<IgnisShieldPower>(Creature, ShieldDamageCap, Creature, null);
        await CreatureCmd.GainBlock(Creature, BlueFlameTransitionBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(Creature, BlueFlameTransitionStrength, Creature, null);
        await AddBurns(targets, 1);
    }

    private async Task GreatswordQuakeMove(IReadOnlyList<Creature> targets)
    {
        Color quakeTint = CurrentPhaseTint;
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.55f);
        PlayHeavyStrikeBurst(quakeTint, 1.35f);
        PlayTargetFlamePuffs(targets);
        await DamageCmd.Attack(GreatswordQuakeDamage)
            .WithHitCount(GreatswordQuakeHits)
            .OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithHitFx(VfxCmd.heavyBluntPath)
            .WithHitVfxNode(target => NLineBurstVfx.Create(target))
            .Execute(null);
        await AddBurns(targets, 2);
    }

    private async Task ShieldBreakMove(IReadOnlyList<Creature> targets)
    {
        _phaseTurnInterruptionQueued = false;
        _phase = 2;
        _pendingSoulflameOpener = true;
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.7f);
        await PlayPhaseTransitionVfx(_phase, SoulflameTint, 1.65f, true);
        await RemoveSelfDebuffs();
        await PowerCmd.Remove<IgnisShieldPower>(Creature);
        await CreatureCmd.GainBlock(Creature, SoulflameTransitionBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(Creature, SoulflameTransitionStrength, Creature, null);
        await AddDazed(targets, 1);
    }

    private async Task SoulflameSlashMove(IReadOnlyList<Creature> targets)
    {
        _pendingSoulflameOpener = false;
        SfxCmd.Play(CastSfxPath);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.65f);
        PlaySlashPrep(targets, SoulflameTint);
        PlaySelfFlames(SoulflameTint, 1.15f, 1.05f);
        await DamageCmd.Attack(SoulflameSlashDamage)
            .WithHitCount(SoulflameSlashHits)
            .OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithHitFx(VfxCmd.giantHorizontalSlashPath)
            .Execute(null);
        await AddBurns(targets, 2);
        await AddDazed(targets, 2);
    }

    private async Task RageComboMove(IReadOnlyList<Creature> targets)
    {
        PlaySlashPrep(targets, SoulflameTint);
        PlaySelfFlames(SoulflameTint, 1.05f, 0.95f);
        await DamageCmd.Attack(RageComboDamage)
            .WithHitCount(RageComboHits)
            .OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, CustomAttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await AddBurns(targets, 2);
        await HealSelf(6);
    }

    private async Task FlameUppercutMove(IReadOnlyList<Creature> targets)
    {
        Color uppercutTint = CurrentPhaseTint;
        PlayChargePrep(uppercutTint);
        await DamageCmd.Attack(FlameUppercutDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, CustomAttackSfx)
            .AfterAttackerAnim(() =>
            {
                PlayHeavyStrikeBurst(uppercutTint, 1.2f);
                return Task.CompletedTask;
            })
            .WithHitFx(VfxCmd.heavyBluntPath)
            .WithHitVfxNode(target => NLineBurstVfx.Create(target))
            .Execute(null);
        await PowerCmd.Apply<VulnerablePower>(targets, 1m, Creature, null);
        await AddDazed(targets, 1);
    }

    private async Task RemoveSelfDebuffs()
    {
        await PowerCmd.Remove<WeakPower>(Creature);
        await PowerCmd.Remove<VulnerablePower>(Creature);
        await PowerCmd.Remove<FrailPower>(Creature);
    }

    private async Task AddBurns(IReadOnlyList<Creature> targets, int amount)
    {
        var aliveTargets = targets.Where(target => target != null && target.IsAlive).ToList();
        if (aliveTargets.Count == 0 || amount <= 0)
        {
            return;
        }

        await CardPileCmd.AddToCombatAndPreview<Burn>(aliveTargets, PileType.Discard, amount, addedByPlayer: false);
    }

    private async Task AddDazed(IReadOnlyList<Creature> targets, int amount)
    {
        var aliveTargets = targets.Where(target => target != null && target.IsAlive).ToList();
        if (aliveTargets.Count == 0 || amount <= 0)
        {
            return;
        }

        await CardPileCmd.AddToCombatAndPreview<Dazed>(aliveTargets, PileType.Discard, amount, addedByPlayer: false);
    }

    private async Task HealSelf(int amount)
    {
        if (Creature == null || amount <= 0 || Creature.IsDead)
        {
            return;
        }

        await CreatureCmd.Heal(Creature, amount);
    }

    private void UpdatePhaseVisual(int phase, bool playTransition = false, Color? flashTint = null)
    {
        var creatureNode = Creature == null ? null : NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode?.Visuals.GetCurrentBody() is not Sprite2D body)
        {
            return;
        }

        string visualPath = phase switch
        {
            2 => PhaseTwoVisualPath,
            3 => PhaseThreeVisualPath,
            _ => PhaseOneVisualPath
        };

        if (LoadPhaseTexture(visualPath) is not { } texture)
        {
            MainFile.Logger.Warn($"Ignis: failed to load phase texture '{visualPath}'");
            return;
        }

        body.Texture = texture;

        if (!playTransition)
        {
            return;
        }

        Vector2 baseScale = body.Scale;
        Color transitionColor = flashTint?.Lightened(0.45f) ?? Colors.White;
        body.Modulate = transitionColor;
        Tween tween = body.CreateTween();
        tween.TweenProperty(body, "scale", baseScale * 1.1f, 0.08f).From(baseScale * 0.9f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(body, "scale", baseScale, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        tween.Parallel().TweenProperty(body, "modulate", Colors.White, 0.22f)
            .From(transitionColor);
    }

    private static Texture2D? LoadPhaseTexture(string visualPath)
    {
        return GD.Load<Texture2D>(visualPath);
    }

    private static float NextShakeAngle()
    {
        return 180f + MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-10f, 10f);
    }

    private Creature? GetPrimaryTarget(IReadOnlyList<Creature> targets)
    {
        return targets.FirstOrDefault(target => target != null && target.IsAlive);
    }

    private void AddVfxToCreature(Creature? target, Node? vfxNode)
    {
        if (target == null || vfxNode == null)
        {
            return;
        }

        NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(vfxNode);
    }

    private void AddVfxToCombat(Node? vfxNode)
    {
        if (vfxNode == null)
        {
            return;
        }

        NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(vfxNode);
    }

    private Vector2? GetFloorPosition(Creature? target)
    {
        return target == null ? null : NCombatRoom.Instance?.GetCreatureNode(target)?.GetBottomOfHitbox();
    }

    private Node2D? CreateSlashImpact(Creature target, Color tint, float rotationDegrees)
    {
        var center = NCombatRoom.Instance?.GetCreatureNode(target)?.VfxSpawnPosition;
        return center == null ? null : NBigSlashImpactVfx.Create(center.Value, rotationDegrees, tint);
    }

    private void PlaySlashPrep(IReadOnlyList<Creature> targets, Color tint)
    {
        var primaryTarget = GetPrimaryTarget(targets);
        if (primaryTarget == null || NCombatRoom.Instance?.GetCreatureNode(primaryTarget) is not { } creatureNode)
        {
            return;
        }

        AddVfxToCreature(primaryTarget, NBigSlashVfx.Create(creatureNode.VfxSpawnPosition, primaryTarget.IsEnemy, tint));
    }

    private void PlayChargePrep(Color tint)
    {
        if (Creature == null)
        {
            return;
        }

        AddVfxToCreature(Creature, NHorizontalLinesVfx.Create(tint, 1.15f, movingRightwards: Creature.IsEnemy));
    }

    private void PlayTargetFlamePuffs(IEnumerable<Creature> targets)
    {
        foreach (var target in targets.Where(target => target != null && target.IsAlive))
        {
            AddVfxToCreature(target, NFireSmokePuffVfx.Create(target));
        }
    }

    private void PlaySelfFlames(Color tint, float burstScale, float burningScale)
    {
        if (Creature == null)
        {
            return;
        }

        if (GetFloorPosition(Creature) is not { } floorPosition)
        {
            return;
        }

        AddVfxToCombat(NFireBurstVfx.Create(floorPosition, burstScale, tint));
        AddVfxToCombat(NFireBurningVfx.Create(floorPosition, burningScale, goingRight: true, tint));
        AddVfxToCombat(NFireBurningVfx.Create(floorPosition, burningScale, goingRight: false, tint));
    }

    private void PlayHeavyStrikeBurst(Color tint, float burstScale)
    {
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal, NextShakeAngle());
        PlaySelfFlames(tint, burstScale, burstScale * 0.9f);
    }

    private async Task PlayPhaseTransitionVfx(int nextPhase, Color tint, float burstScale, bool shatteredShield)
    {
        if (Creature == null)
        {
            return;
        }

        AddVfxToCreature(Creature, NHorizontalLinesVfx.Create(tint, 1.35f, movingRightwards: Creature.IsEnemy));
        PlaySelfFlames(tint, burstScale * 0.8f, burstScale * 0.55f);
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Center);
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal, NextShakeAngle());
        await Cmd.Wait(0.08f);

        UpdatePhaseVisual(nextPhase, playTransition: true, flashTint: tint);
        SfxCmd.Play(AttackSfxPath);
        SfxCmd.Play(shatteredShield ? ShieldBreakSfxPath : ShieldAwakeningSfxPath);
        VfxCmd.PlayOnCreatureCenter(Creature, VfxCmd.screamVfx);
        AddVfxToCreature(Creature, NLineBurstVfx.Create(Creature));
        PlaySelfFlames(tint, burstScale, burstScale * 0.9f);
        AddVfxToCreature(Creature, NHorizontalLinesVfx.Create(tint, 1.6f, movingRightwards: !Creature.IsEnemy));

        if (shatteredShield)
        {
            AddVfxToCreature(Creature, CreateSlashImpact(Creature, tint, 90f));
            NGame.Instance?.DoHitStop(ShakeStrength.Strong, ShakeDuration.Normal);
        }
        else
        {
            NGame.Instance?.DoHitStop(ShakeStrength.Medium, ShakeDuration.Short);
        }

        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short, NextShakeAngle());
        await Cmd.Wait(shatteredShield ? 0.26f : 0.2f);
    }
}
