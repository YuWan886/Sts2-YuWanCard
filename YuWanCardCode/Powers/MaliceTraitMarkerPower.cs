using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public sealed class MaliceTraitMarkerPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TraitCount", 1)
    ];

    protected override bool IsVisibleInternal => false;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars["TraitCount"].BaseValue = Amount;
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            DynamicVars["TraitCount"].BaseValue = Amount;
        }

        return Task.CompletedTask;
    }
}
