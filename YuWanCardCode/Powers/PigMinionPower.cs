using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Powers;

public class PigMinionPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BonusBlock", 1m)
    ];

    public int BonusBlock => DynamicVars["BonusBlock"].IntValue;

    private decimal _storedBlockToUse = 0m;
    internal bool _isBeingRemoved = false;

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
        if (creature != Owner)
        {
            return true;
        }
        return false;
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
        if (_isBeingRemoved) return;

        _isBeingRemoved = true;
        try
        {
            var ownerCreature = Owner.PetOwner?.Creature;
            if (ownerCreature != null)
            {
                var pigFriendsPower = ownerCreature.GetPower<PigFriendsPower>();
                if (pigFriendsPower != null && !pigFriendsPower._isBeingRemoved)
                {
                    pigFriendsPower._isBeingRemoved = true;
                    await PowerCmd.Remove(pigFriendsPower);
                }
            }
        }
        finally
        {
            _isBeingRemoved = false;
        }
    }

    private bool _isProcessingStrengthChange = false;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner.PetOwner?.Creature) return;
        if (power is not StrengthPower) return;
        if (amount <= 0) return;
        if (Owner.IsDead) return;
        if (_isProcessingStrengthChange) return;

        _isProcessingStrengthChange = true;
        try
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 1, Owner.PetOwner?.Creature, cardSource);
        }
        finally
        {
            _isProcessingStrengthChange = false;
        }
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner.PetOwner?.Creature) return;
        if (Owner.IsDead) return;
        if (amount <= 0) return;

        Flash();
        await CreatureCmd.GainBlock(Owner, BonusBlock, ValueProp.Unpowered, null);
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (Owner.IsDead) return;
        if (CombatManager.Instance?.IsEnding != false) return;

        Flash();
        await ApplyRandomBuffToAllPlayers();
    }

    private async Task ApplyRandomBuffToAllPlayers()
    {
        if (CombatState == null) return;

        var rng = CombatState.RunState?.Rng?.Shuffle;
        if (rng == null) return;

        var players = CombatState.Players;
        if (players == null || players.Count == 0) return;

        foreach (var player in players.ToList())
        {
            if (player == null || player.Creature == null || player.Creature.IsDead) continue;
            if (CombatManager.Instance?.IsEnding != false) return;

            int buffType = rng.NextInt(5);
            var creature = player.Creature;
            switch (buffType)
            {
                case 0:
                    await PlayerCmd.GainEnergy(1, player);
                    break;
                case 1:
                    await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), creature, 1, Owner, null);
                    break;
                case 2:
                    await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), creature, 1, Owner, null);
                    break;
                case 3:
                    await CreatureCmd.Heal(creature, 1);
                    break;
                case 4:
                    await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, player);
                    break;
            }
        }
    }
}
