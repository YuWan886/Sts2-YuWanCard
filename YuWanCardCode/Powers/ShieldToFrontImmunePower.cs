using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class ShieldToFrontImmunePower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => $"res://YuWanCard/images/powers/shield_to_front_power.png";
    public override string CustomBigIconPath => $"res://YuWanCard/images/powers/shield_to_front_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 1m)];

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

        var combatState = CombatState;
        if (combatState == null) return amount;

        if (combatState.CurrentSide != CombatSide.Enemy) return amount;

        Flash();
        return 0m;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Enemy) return;

        Flash();
        await PowerCmd.Remove(this);
    }
}
