using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class PigDefectionPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PigDefectionPower", 1)
    ];

    private decimal _storedBlockToUse = 0m;

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

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (target == null) return 1m;

        if (target.Side == CombatSide.Player)
        {
            return 0m;
        }

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

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (Owner.IsDead) return;
        if (CombatManager.Instance?.IsEnding != false) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var rng = Owner.Player?.RunState?.Rng?.Niche ?? Owner.PetOwner?.RunState?.Rng?.Niche;
        if (rng == null) return;

        var target = rng.NextItem(enemies);
        if (target == null) return;

        Flash();

        var strengthPower = Owner.GetPower<StrengthPower>();
        int damage = 7 + (strengthPower?.Amount ?? 0);

        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, damage, ValueProp.Move, Owner);
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature == Owner;
    }

}
