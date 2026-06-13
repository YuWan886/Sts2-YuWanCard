using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Combat;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PrideComesBeforeFallPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://YuWanCard/images/powers/sad_army_win_power.png";
    public override string? CustomBigIconPath => CustomPackedIconPath;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player == null)
        {
            return Task.CompletedTask;
        }

        RelicReward? reward = RelicRewardUtils.CreateStandardSharedRelicReward(Owner.Player);
        if (reward == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        room.AddExtraReward(Owner.Player, reward);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
