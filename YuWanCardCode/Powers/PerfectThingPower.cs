using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace YuWanCard.Powers;

public class PerfectThingPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => false;

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return Amount > 0 && player == Owner.Player;
    }

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
