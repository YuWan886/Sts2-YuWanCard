using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class HorizonPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HorizonPower", 1m)];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner.Creature != Owner)
        {
            modifiedCost = originalCost;
            return false;
        }

        var reduction = Amount;
        modifiedCost = System.Math.Max(0m, originalCost - reduction);
        return true;
    }
}
