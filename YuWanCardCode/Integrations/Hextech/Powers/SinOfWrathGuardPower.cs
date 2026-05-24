using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public class SinOfWrathGuardPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    protected override string IconBasePath => "res://YuWanCard/images/integrations/hextech/powers/sin_of_wrath_guard_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("GuardAmount", 2m)];

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0 || amount <= 0)
        {
            return amount;
        }

        int absorbed = Math.Min(Amount, (int)decimal.Ceiling(amount));
        if (absorbed <= 0)
        {
            return amount;
        }

        SetAmount(Amount - absorbed);
        return Math.Max(0m, amount - absorbed);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side || Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Remove(this);
    }
}
