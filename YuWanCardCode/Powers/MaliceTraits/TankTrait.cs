using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
namespace YuWanCard.Powers.MaliceTraits;

public sealed class TankTrait : MaliceTraitPowerBase
{
    private const decimal HpMultiplier = 0.25m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TankPercent", 25)
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        int bonusHp = (int)Math.Ceiling(Owner.MaxHp * HpMultiplier);
        if (bonusHp <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainMaxHp(Owner, bonusHp);
    }
}
