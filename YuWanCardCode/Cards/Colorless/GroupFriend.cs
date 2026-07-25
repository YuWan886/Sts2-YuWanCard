using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class GroupFriend : YuWanCardModel
{
    public GroupFriend() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<GroupFriendPower>(5, 3);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<GroupFriendPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["GroupFriendPower"].IntValue,
            Owner.Creature,
            this);
    }
}
