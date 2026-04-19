using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class ShieldToFrontPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageMultiplier", 2m)];

    public decimal DamageMultiplier => DynamicVars["DamageMultiplier"].BaseValue;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return 1m;
        if (dealer == null) return 1m;
        if (dealer.Side == Owner.Side) return 1m;

        var combatState = CombatState;
        if (combatState == null) return 1m;

        if (combatState.CurrentSide != CombatSide.Enemy) return 1m;

        Flash();
        return DamageMultiplier;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Enemy) return;

        Flash();
        await PowerCmd.Remove(this);
    }
}
