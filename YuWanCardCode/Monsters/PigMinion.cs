using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace YuWanCard.Monsters;

public sealed class PigMinion : MonsterModel
{
    public const float AttackerAnimDelay = 0.3f;
    public const string AttackAnim = "attack";
    public new const string AttackSfx = "event:/sfx/characters/osty/osty_attack";

    protected override string VisualsPath => "res://YuWanCard/scenes/monsters/pig_minion.tscn";

    public override int MinInitialHp => 5;
    public override int MaxInitialHp => 5;

    public override string DeathSfx => "event:/sfx/characters/osty/osty_die";
    public override bool IsHealthBarVisible => Creature.IsAlive;

    private static int AttackDamage => 4;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attackMove = new("ATTACK_MOVE", AttackMove, new SingleAttackIntent(AttackDamage));
        attackMove.FollowUpState = attackMove;

        return new MonsterMoveStateMachine([attackMove], attackMove);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        if (targets.Count > 0)
        {
            var target = targets[0];
            await DamageCmd.Attack(AttackDamage)
                .FromMonster(this)
                .WithAttackerAnim(AttackAnim, 0.15f)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idleAnim = new AnimState("idle", true);
        var deadAnim = idleAnim;
        var hitAnim = idleAnim;
        var attackAnim = idleAnim;
        var castAnim = idleAnim;

        var animator = new CreatureAnimator(idleAnim, controller);

        animator.AddAnyState("Idle", idleAnim);
        animator.AddAnyState("Dead", deadAnim);
        animator.AddAnyState("Hit", hitAnim);
        animator.AddAnyState("Attack", attackAnim);
        animator.AddAnyState("Cast", castAnim);

        return animator;
    }
}
