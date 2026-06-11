using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigLeaderPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var pig = PetManager.FindPetByType<PigMinion>(Owner);
        if (pig is not { IsAlive: true })
        {
            Flash();
            await PowerCmd.Apply<PigFriendsPower>(Owner, 1, Owner, null);
            pig = PetManager.FindPetByType<PigMinion>(Owner);
        }

        if (pig is { IsAlive: true } && Amount > 0)
        {
            Flash();
            await PowerCmd.Apply<PigLeaderBoostPower>(pig, Amount, Owner, null);
        }
    }
}
