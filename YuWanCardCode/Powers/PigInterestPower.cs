using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public sealed class PigInterestPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();

        int gainedGold = (int)Math.Floor(player.Gold * Amount / 100m);
        if (gainedGold > 0)
        {
            await PlayerCmd.GainGold(gainedGold, player);
        }
    }
}
