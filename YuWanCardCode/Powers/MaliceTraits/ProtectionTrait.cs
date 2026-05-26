using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ProtectionTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PlatingCount", 5m)];
    protected override string[] AutoUpdateVarNames => ["PlatingCount"];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        Flash();
        await PowerCmd.Apply<PlatingPower>(Owner, Amount * 5, Owner, null);
    }
}
