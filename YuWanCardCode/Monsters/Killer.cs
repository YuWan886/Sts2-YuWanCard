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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Monsters;

public sealed class Killer : YuWanMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 180);

    public override int MaxInitialHp => MinInitialHp;

    private static int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private static int MultiDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private static int MultiRepeat => 3;

    private static int ZoomDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private static int ZoomBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 10);

    private static int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private static int DazedCount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1);

    private static int HardenedShellAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 40);

    private static int PersonalHiveAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1);

    private static int SkittishAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 10);

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override string? CustomDeathSfx => "event:/sfx/enemy/enemy_attacks/hunter_killer/hunter_killer_die";

    private int _enlargeTriggers;

    public float CurrentScale { get; private set; } = 1f;

    private static void PlayTalkLine(LocString line, Creature speaker)
    {
        GameVersionCompat.TalkCmdPlay(line, speaker);
    }

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
        if (Creature.IsDead)
        {
            return;
        }
        if (!CombatState.Players.All(p => p.Creature.IsDead))
        {
            return;
        }
        LocString line = L10NMonsterLookup("KILLER.onPlayerDeath.speakLine");
        PlayTalkLine(line, Creature);
    }

    public override Task BeforeDeath(Creature creature)
    {
        if (creature != Creature)
        {
            return Task.CompletedTask;
        }
        LocString line = L10NMonsterLookup("KILLER.onDeath.speakLine");
        PlayTalkLine(line, Creature);
        return Task.CompletedTask;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = [];

        MoveState sleepMove = new("SLEEP_MOVE", SleepMove, new SleepIntent());
        MoveState wakeMove = new("WAKE_MOVE", WakeMove, new BuffIntent());
        MoveState slashMove = new("SLASH_MOVE", SlashMove, new SingleAttackIntent(SlashDamage));
        MoveState multiAttackMove = new("MULTI_ATTACK_MOVE", MultiAttackMove, new MultiAttackIntent(MultiDamage, MultiRepeat));
        MoveState goopMove = new("GOOP_MOVE", GoopMove, new DebuffIntent());
        MoveState zoomMove = new("ZOOM_MOVE", ZoomMove, new SingleAttackIntent(ZoomDamage), new DefendIntent());
        MoveState enlargeMove = new("ENLARGE_MOVE", EnlargeMove, new BuffIntent(), new StatusIntent(DazedCount));

        sleepMove.FollowUpState = wakeMove;
        wakeMove.FollowUpState = slashMove;

        RandomBranchState randomBranch = new("RAND");
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
        LocString line = L10NMonsterLookup("KILLER.moves.SLEEP.speakLine");
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
        await PowerCmd.Apply<PersonalHivePower>(Creature, PersonalHiveAmount, Creature, null);
        await PowerCmd.Apply<SkittishPower>(Creature, SkittishAmount, Creature, null);
        LocString line = L10NMonsterLookup("KILLER.moves.WAKE.speakLine");
        PlayTalkLine(line, Creature);
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

        // 只给存活的目标添加 Dazed 牌
        var aliveTargets = targets.Where(t => t != null && t.IsAlive).ToList();
        if (aliveTargets.Count > 0)
        {
            await CardPileCmd.AddToCombatAndPreview<Dazed>(aliveTargets, PileType.Discard, DazedCount, addedByPlayer: false);
        }

        _enlargeTriggers++;
        CurrentScale = 1f + 0.08f * _enlargeTriggers;
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.SetDefaultScaleTo(CurrentScale, 0.5f);
    }

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(controller,
            idleName: "idle_loop",
            deadName: "die",
            deadLoop: false,
            hitName: "hurt",
            hitLoop: false,
            attackName: "attack",
            attackLoop: false,
            castName: "cast",
            castLoop: false);
    }
}
