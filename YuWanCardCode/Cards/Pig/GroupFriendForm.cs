using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class GroupFriendForm : YuWanCardModel
{
    public GroupFriendForm() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<GroupFriendFormPower>(1);
        WithKeywords(CardKeyword.Retain);
        WithCostUpgradeBy(-1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<GroupFriendFormPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["GroupFriendFormPower"].IntValue, Owner.Creature, this);
    }
}
