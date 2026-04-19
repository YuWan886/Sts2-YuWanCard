using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class EmperorsNewPigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 1m)];

    [SavedProperty]
    public bool YUWANCARD_PreventDebuffs { get; set; } = false;

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (cardSource is { IsUpgraded: true })
        {
            YUWANCARD_PreventDebuffs = true;
        }
        await base.BeforeApplied(target, amount, applier, cardSource);
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (!YUWANCARD_PreventDebuffs)
        {
            return false;
        }
        if (target != Owner)
        {
            return false;
        }
        if (canonicalPower.Type != PowerType.Debuff)
        {
            return false;
        }
        if (!canonicalPower.IsVisible)
        {
            return false;
        }

        Flash();
        modifiedAmount = 0m;
        return true;
    }

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress)
        {
            return amount;
        }
        if (target != Owner)
        {
            return amount;
        }
        if (dealer == null)
        {
            return amount;
        }
        if (dealer.Side == Owner.Side)
        {
            return amount;
        }

        Flash();
        return 0m;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Enemy) return;

        Flash();
        SetAmount(Amount - 1);

        if (Amount <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
