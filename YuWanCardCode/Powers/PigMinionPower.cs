using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class PigMinionPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private new const string IconPath = "res://YuWanCard/images/powers/pig_strength_power.png";
    public override string? CustomPackedIconPath => IconPath;
    public override string? CustomBigIconPath => IconPath;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BonusBlock", 1m)
    ];

    public int BonusBlock => DynamicVars["BonusBlock"].IntValue;

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

    public override bool ShouldAllowHitting(Creature creature)
    {
        return creature.IsAlive;
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature == Owner;
    }

    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return false;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner.PetOwner?.Creature) return 0m;
        if (Owner.IsDead) return 0m;
        if (!props.IsPoweredAttack()) return 0m;

        var strengthPower = Owner.GetPower<StrengthPower>();
        if (strengthPower == null) return 0m;

        return strengthPower.Amount;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Owner) return;

        var ownerCreature = Owner.PetOwner?.Creature;
        if (ownerCreature != null)
        {
            var pigFriendsPower = ownerCreature.GetPower<PigFriendsPower>();
            if (pigFriendsPower != null)
            {
                await PowerCmd.Remove(pigFriendsPower);
            }
        }
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner.PetOwner?.Creature) return;
        if (power is not StrengthPower) return;
        if (amount <= 0) return;
        if (Owner.IsDead) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner, 1, Owner.PetOwner?.Creature, cardSource);
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner.PetOwner?.Creature) return;
        if (Owner.IsDead) return;
        if (amount <= 0) return;

        Flash();
        await CreatureCmd.GainBlock(Owner, BonusBlock, ValueProp.Unpowered, null);
    }
}
