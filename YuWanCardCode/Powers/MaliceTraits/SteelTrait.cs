using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SteelTrait : MaliceTraitPowerBase
{
    private const int MaxArtifact = 2;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int current = Owner.GetPower<ArtifactPower>()?.Amount ?? 0;
        int toApply = Math.Min((int)Amount, Math.Max(0, MaxArtifact - current));
        if (toApply <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<ArtifactPower>(Owner, toApply, Owner, null);
    }
}
