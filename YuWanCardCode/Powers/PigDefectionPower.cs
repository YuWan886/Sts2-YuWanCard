using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigDefectionPower : YuWanPowerModel
{
    private const int AttackDamage = 6;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PigDefectionPower", 1)
    ];

    private decimal _storedBlockToUse = 0m;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SetAttackIntent();
        return Task.CompletedTask;
    }

    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal _, ValueProp props, Creature? __)
    {
        if (target != Owner.PetOwner?.Creature) return target;
        if (Owner.IsDead) return target;
        if (!props.IsPoweredAttack()) return target;

        _storedBlockToUse = Owner.Block;
        return Owner;
    }

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return amount;
        if (_storedBlockToUse <= 0) return amount;

        decimal blockedAmount = Math.Min(_storedBlockToUse, amount);
        decimal remainingDamage = amount - blockedAmount;

        if (blockedAmount > 0)
        {
            Owner.LoseBlockInternal(blockedAmount);
            _storedBlockToUse = Math.Max(0, _storedBlockToUse - blockedAmount);
        }

        return remainingDamage;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        return 1m;
    }

    public override bool ShouldAllowHitting(Creature creature)
    {
        if (creature != Owner)
        {
            return true;
        }
        return creature.IsAlive;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (Owner.IsDead) return;
        if (CombatManager.Instance?.IsEnding != false) return;

        var targetOwner = Owner.Player ?? Owner.PetOwner;
        if (targetOwner == null) return;

        var target = CombatTargetingUtils.GetDeterministicRandomTarget(targetOwner, combatState.HittableEnemies);
        if (target == null) return;

        Flash();

        var strengthPower = Owner.GetPower<StrengthPower>();
        int damage = AttackDamage + (strengthPower?.Amount ?? 0);

        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, damage, ValueProp.Move, Owner);

        SetAttackIntent();
    }

    private void SetAttackIntent()
    {
        if (Owner.Monster == null) return;

        var attackMove = new MoveState(
            "ATTACK_MOVE",
            _ => Task.CompletedTask,
            new SingleAttackIntent(() => AttackDamage + (Owner?.GetPower<StrengthPower>()?.Amount ?? 0))
        );
        attackMove.FollowUpState = attackMove;

        Owner.Monster.SetMoveImmediate(attackMove, forceTransition: true);
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature == Owner;
    }

}
