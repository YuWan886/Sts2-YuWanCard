using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace YuWanCard.Powers;

public class RetainEnergyNextTurnPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        if (Owner.Player == null || player != Owner.Player || Amount <= 0)
            return true;

        return false;
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (Owner.Player == null || player != Owner.Player || Amount <= 0)
            return;

        Flash();
        await PowerCmd.Remove(this);
    }
}
