using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;
using YuWanCard.Cards;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Monsters;

public sealed class FerrousWroughtnaut : YuWanMonsterModel
{
    private const int ArtifactAmount = 5;
    private const int HorizontalSlashHitsBelowHalfHp = 3;
    private static readonly Vector2 BaseVisualScale = new(0.48f, 0.48f);
    private const string SleepingVisualPath = "res://YuWanCard/images/monsters/ferrous_wroughtnaut/ferrous_wroughtnaut_1.png";
    private const string AwakeVisualPath = "res://YuWanCard/images/monsters/ferrous_wroughtnaut/ferrous_wroughtnaut_2.png";
    private const string StaggeredVisualPath = "res://YuWanCard/images/monsters/ferrous_wroughtnaut/ferrous_wroughtnaut_3.png";
    private static readonly Color SteelTint = new("A8B6C8");

    private MoveState? _horizontalSlashMove;
    private MoveState? _horizontalMultiSlashMove;
    private bool _isAwake;
    private bool _isStaggered;
    private GuardianVisual? _currentVisual;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 145, 135);

    public override int MaxInitialHp => MinInitialHp;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.ArmorBig;

    private static int HorizontalSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);

    private static int HorizontalSlashMultiDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private static int VerticalSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 17);

    public bool IsStaggered => _isStaggered;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<FerrousWroughtnautPositioningPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Creature, ArtifactAmount, Creature, null);
        UpdateVisual(GuardianVisual.Sleeping);
    }

    public override async Task BeforeCombatStartLate()
    {
        await base.BeforeCombatStartLate();
        FerrousWroughtnautPositioning.Initialize(this, CombatState.Players);
        foreach (var player in CombatState.Players.Where(static player => player.Creature.IsAlive))
        {
            List<CardPileAddResult> addResults = [];
            for (int index = 0; index < 3; index++)
            {
                CardPileAddResult drawResult = await CardPileCmd.AddGeneratedCardToCombat(
                    CombatState.CreateCard<FerrousWroughtnautShift>(player), PileType.Draw, player);
                CardPileAddResult discardResult = await CardPileCmd.AddGeneratedCardToCombat(
                    CombatState.CreateCard<FerrousWroughtnautShift>(player), PileType.Discard, player);
                addResults.Add(drawResult);
                addResults.Add(discardResult);
            }

            if (addResults.Count > 0)
            {
                CardCmd.PreviewCardPileAdd(addResults, 1.5f);
            }
        }
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_isAwake || target != Creature || dealer?.Side != CombatSide.Player || cardSource?.Type != CardType.Attack)
        {
            return;
        }

        _isAwake = true;
        _isStaggered = false;
        await PowerCmd.Remove<ArtifactPower>(Creature);
        UpdateVisual(GuardianVisual.Awake);

        // Force the attack state now so the displayed intent changes as soon as the guardian wakes.
        MoveState? horizontalSlashMove = GetHorizontalSlashMoveForCurrentHp();
        if (horizontalSlashMove != null)
        {
            SetMoveImmediate(horizontalSlashMove, forceTransition: true);
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Creature || dealer?.Side != CombatSide.Player)
        {
            return 1m;
        }

        return FerrousWroughtnautPositioning.CanDamage(this, dealer) ? 1m : 0m;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && participants.Contains(Creature))
        {
            FerrousWroughtnautPositioning.TurnTowardMostPlayers(this);
        }

        return Task.CompletedTask;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState sleepMove = new("SLEEP_MOVE", SleepMove, new SleepIntent());
        _horizontalSlashMove = new MoveState(
            "HORIZONTAL_SLASH_MOVE",
            HorizontalSlashMove,
            new SingleAttackIntent(HorizontalSlashDamage));
        _horizontalMultiSlashMove = new MoveState(
            "HORIZONTAL_MULTI_SLASH_MOVE",
            HorizontalMultiSlashMove,
            new MultiAttackIntent(HorizontalSlashMultiDamage, HorizontalSlashHitsBelowHalfHp));
        MoveState verticalSlashMove = new("VERTICAL_SLASH_MOVE", VerticalSlashMove, new SingleAttackIntent(VerticalSlashDamage));
        MoveState staggerFirstMove = new("STAGGER_FIRST_MOVE", StaggerFirstMove, new StunIntent());
        MoveState staggerSecondMove = new("STAGGER_SECOND_MOVE", StaggerSecondMove, new StunIntent());
        ConditionalBranchState horizontalSlashSelection = new("HORIZONTAL_SLASH_SELECTION");

        sleepMove.FollowUpState = sleepMove;
        _horizontalSlashMove.FollowUpState = verticalSlashMove;
        _horizontalMultiSlashMove.FollowUpState = verticalSlashMove;
        verticalSlashMove.FollowUpState = staggerFirstMove;
        staggerFirstMove.FollowUpState = staggerSecondMove;
        staggerSecondMove.FollowUpState = horizontalSlashSelection;
        horizontalSlashSelection.AddState(_horizontalSlashMove, () => !IsBelowHalfHp());
        horizontalSlashSelection.AddState(_horizontalMultiSlashMove, IsBelowHalfHp);

        return new MonsterMoveStateMachine(
            [sleepMove, _horizontalSlashMove, _horizontalMultiSlashMove, verticalSlashMove, staggerFirstMove, staggerSecondMove, horizontalSlashSelection],
            sleepMove);
    }

    private Task SleepMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual(GuardianVisual.Sleeping);
        return Task.CompletedTask;
    }

    private async Task HorizontalSlashMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual(GuardianVisual.Awake);
        var frontPlayers = FerrousWroughtnautPositioning.GetFrontPlayers(this).ToList();
        await PlayHorizontalSlashVfx(frontPlayers);
        await DamageFrontPlayers(frontPlayers, HorizontalSlashDamage, 1);
    }

    private async Task HorizontalMultiSlashMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual(GuardianVisual.Awake);
        var frontPlayers = FerrousWroughtnautPositioning.GetFrontPlayers(this).ToList();
        await PlayHorizontalSlashVfx(frontPlayers);
        await DamageFrontPlayers(frontPlayers, HorizontalSlashMultiDamage, HorizontalSlashHitsBelowHalfHp);
    }

    private async Task VerticalSlashMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual(GuardianVisual.Awake);
        await DamageFrontPlayers(FerrousWroughtnautPositioning.GetFrontPlayers(this), VerticalSlashDamage, 1);
        _isStaggered = true;
        UpdateVisual(GuardianVisual.Staggered);
    }

    private Task StaggerFirstMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual(GuardianVisual.Staggered);
        return Task.CompletedTask;
    }

    private Task StaggerSecondMove(IReadOnlyList<Creature> targets)
    {
        _isStaggered = false;
        UpdateVisual(GuardianVisual.Awake);
        return Task.CompletedTask;
    }

    private bool IsBelowHalfHp()
    {
        return Creature.CurrentHp * 2 < Creature.MaxHp;
    }

    private MoveState? GetHorizontalSlashMoveForCurrentHp()
    {
        return IsBelowHalfHp() ? _horizontalMultiSlashMove : _horizontalSlashMove;
    }

    private void UpdateVisual(GuardianVisual visual)
    {
        if (_currentVisual == visual)
        {
            return;
        }

        if (NCombatRoom.Instance?.GetCreatureNode(Creature)?.Visuals.GetCurrentBody() is not Sprite2D body)
        {
            return;
        }

        string texturePath = visual switch
        {
            GuardianVisual.Awake => AwakeVisualPath,
            GuardianVisual.Staggered => StaggeredVisualPath,
            _ => SleepingVisualPath
        };
        Texture2D? texture = GD.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            MainFile.Logger.Warn($"FerrousWroughtnaut: failed to load visual '{texturePath}'");
            return;
        }

        if (visual != GuardianVisual.Sleeping)
        {
            ResetBodyTransform(body);
        }

        body.Texture = texture;
        _currentVisual = visual;
    }

    private static void ResetBodyTransform(Sprite2D body)
    {
        body.Scale = BaseVisualScale;
        body.Skew = 0f;
        body.Rotation = 0f;
        body.Modulate = Colors.White;
    }

    private Task PlayHorizontalSlashVfx(IReadOnlyList<Creature> targets)
    {
        foreach (Creature target in targets.Where(static target => target != null && target.IsAlive))
        {
            if (NCombatRoom.Instance?.GetCreatureNode(target) is { } targetNode)
            {
                AddCombatVfx(NBigSlashVfx.Create(targetNode.VfxSpawnPosition, target.IsEnemy, SteelTint));
            }
        }

        return Task.CompletedTask;
    }

    private async Task DamageFrontPlayers(IEnumerable<Creature> targets, int damage, int hitCount)
    {
        var targetList = targets.Where(static target => target.IsAlive).ToList();
        for (int hit = 0; hit < hitCount && targetList.Count > 0; hit++)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), targetList, damage, ValueProp.Move, Creature);
            targetList.RemoveAll(static target => !target.IsAlive);
        }
    }

    private static void AddCombatVfx(Node? vfx)
    {
        if (vfx != null)
        {
            NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(vfx);
        }
    }

    private enum GuardianVisual
    {
        Sleeping,
        Awake,
        Staggered
    }
}
