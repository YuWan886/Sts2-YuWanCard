using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;
using YuWanCard.Commands;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public class GroupFriendPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<GroupFriendImpact>();

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power != this || Owner?.Player == null)
        {
            return;
        }

        await GroupFriendCmd.RefreshGroupFriend(amount, Owner.Player);
    }
}
