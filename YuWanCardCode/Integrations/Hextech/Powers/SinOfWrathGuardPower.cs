using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.HealthBar;

namespace YuWanCard.Powers;

public class SinOfWrathGuardPower : YuWanPowerModel, IHealthBarOverlaySource
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

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

    public IEnumerable<HealthBarOverlaySegment> GetHealthBarOverlaySegments(HealthBarOverlayContext context)
    {
        if (Amount > 0 && context.Creature == Owner)
        {
            yield return new HealthBarOverlaySegment(
                Amount,
                new Color(1f, 0.84f, 0f), // gold/yellow
                HealthBarOverlayDirection.FromRight);
        }
    }
}
