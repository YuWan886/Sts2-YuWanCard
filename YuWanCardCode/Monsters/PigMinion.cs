using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace YuWanCard.Monsters;

public sealed class PigMinion : YuWanMonsterModel
{
    public const float AttackerAnimDelay = 0.3f;
    public const string AttackAnim = "attack";
    public new const string AttackSfx = "event:/sfx/characters/osty/osty_attack";

    public override int MinInitialHp => 5;
    public override int MaxInitialHp => 5;

    public override string? CustomAttackSfx => AttackSfx;
    public override string? CustomDeathSfx => "event:/sfx/characters/osty/osty_die";

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

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return null;
    }
}
