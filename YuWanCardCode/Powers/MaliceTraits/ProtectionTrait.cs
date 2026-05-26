using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ProtectionTrait : MaliceTraitPowerBase
{
    public override async Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        Flash();
        await PowerCmd.Apply<PlatingPower>(Owner, Amount * 2, Owner, null);
    }
}
