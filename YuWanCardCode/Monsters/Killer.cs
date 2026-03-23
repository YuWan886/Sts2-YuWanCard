using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Monsters;

public sealed class Killer : MonsterModel
{
    public override string VisualsPath => "res://YuWanCard/scenes/monsters/killer/killer_visuals.tscn";

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 180, 150);

    public override int MaxInitialHp => MinInitialHp;

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private int MultiDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private int MultiRepeat => 3;

    private int ZoomDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int ZoomBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 12);

    private int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private int DazedCount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1);

    private int HardenedShellAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 20);

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override string DeathSfx => "event:/sfx/enemy/enemy_attacks/hunter_killer/hunter_killer_die";

    private int _enlargeTriggers;

    public float CurrentScale { get; private set; } = 1f;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<HardenedShellPower>(Creature, HardenedShellAmount, Creature, null);
        foreach (var player in CombatState.Players)
        {
            player.Creature.Died += OnPlayerDied;
        }
    }

    private void OnPlayerDied(Creature creature)
    {
        creature.Died -= OnPlayerDied;
        if (!CombatState.Players.All(p => p.Creature.IsDead))
        {
            return;
        }
        LocString line = MonsterModel.L10NMonsterLookup("KILLER.onPlayerDeath.speakLine");
        TalkCmd.Play(line, Creature);
    }

    public override Task BeforeDeath(Creature creature)
    {
        if (creature != Creature)
        {
            return Task.CompletedTask;
        }
        LocString line = MonsterModel.L10NMonsterLookup("KILLER.onDeath.speakLine");
        TalkCmd.Play(line, Creature);
        return Task.CompletedTask;
    }

    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        MoveState sleepMove = new MoveState("SLEEP_MOVE", SleepMove, new SleepIntent());
        MoveState wakeMove = new MoveState("WAKE_MOVE", WakeMove, new BuffIntent());
        MoveState slashMove = new MoveState("SLASH_MOVE", SlashMove, new SingleAttackIntent(SlashDamage));
        MoveState multiAttackMove = new MoveState("MULTI_ATTACK_MOVE", MultiAttackMove, new MultiAttackIntent(MultiDamage, MultiRepeat));
        MoveState goopMove = new MoveState("GOOP_MOVE", GoopMove, new DebuffIntent());
        MoveState zoomMove = new MoveState("ZOOM_MOVE", ZoomMove, new SingleAttackIntent(ZoomDamage), new DefendIntent());
        MoveState enlargeMove = new MoveState("ENLARGE_MOVE", EnlargeMove, new BuffIntent(), new StatusIntent(DazedCount));

        sleepMove.FollowUpState = wakeMove;
        wakeMove.FollowUpState = slashMove;

        RandomBranchState randomBranch = new RandomBranchState("RAND");
        slashMove.FollowUpState = randomBranch;
        multiAttackMove.FollowUpState = randomBranch;
        goopMove.FollowUpState = randomBranch;
        zoomMove.FollowUpState = randomBranch;
        enlargeMove.FollowUpState = randomBranch;

        randomBranch.AddBranch(slashMove, MoveRepeatType.CannotRepeat);
        randomBranch.AddBranch(multiAttackMove, 2);
        randomBranch.AddBranch(goopMove, MoveRepeatType.CannotRepeat);
        randomBranch.AddBranch(zoomMove, 2);
        randomBranch.AddBranch(enlargeMove, 1);

        list.Add(sleepMove);
        list.Add(wakeMove);
        list.Add(slashMove);
        list.Add(multiAttackMove);
        list.Add(goopMove);
        list.Add(zoomMove);
        list.Add(enlargeMove);
        list.Add(randomBranch);

        return new MonsterMoveStateMachine(list, sleepMove);
    }

    private async Task SleepMove(IReadOnlyList<Creature> targets)
    {
        LocString line = MonsterModel.L10NMonsterLookup("KILLER.moves.SLEEP.speakLine");
        ThinkCmd.Play(line, Creature);
        await Cmd.Wait(0.5f);
    }

    private async Task WakeMove(IReadOnlyList<Creature> targets)
    {
        if (TestMode.IsOff)
        {
            NRunMusicController.Instance?.TriggerEliteSecondPhase();
        }
        await PowerCmd.Apply<StrengthPower>(Creature, 8m, Creature, null);
        LocString line = MonsterModel.L10NMonsterLookup("KILLER.moves.WAKE.speakLine");
        TalkCmd.Play(line, Creature);
        await Cmd.Wait(0.5f);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(SlashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await Cmd.Wait(0.25f);
    }

    private async Task MultiAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MultiDamage).WithHitCount(MultiRepeat).OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task GoopMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.4f);
        await PowerCmd.Apply<TenderPower>(targets, 1m, Creature, null);
    }

    private async Task ZoomMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ZoomDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(Creature, ZoomBlock, ValueProp.Move, null);
    }

    private async Task EnlargeMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 1.0f);
        await PowerCmd.Apply<StrengthPower>(Creature, StrengthGain, Creature, null);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, DazedCount, addedByPlayer: false);
        _enlargeTriggers++;
        CurrentScale = 1f + 0.08f * _enlargeTriggers;
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.SetDefaultScaleTo(CurrentScale, 0.5f);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idleState = new AnimState("idle_loop", isLooping: true);
        AnimState castState = new AnimState("cast");
        AnimState attackState = new AnimState("attack");
        AnimState hurtState = new AnimState("hurt");
        AnimState dieState = new AnimState("die");

        castState.NextState = idleState;
        attackState.NextState = idleState;
        hurtState.NextState = idleState;

        CreatureAnimator animator = new CreatureAnimator(idleState, controller);
        animator.AddAnyState("Idle", idleState);
        animator.AddAnyState("Cast", castState);
        animator.AddAnyState("Attack", attackState);
        animator.AddAnyState("Dead", dieState);
        animator.AddAnyState("Hit", hurtState);

        return animator;
    }
}
