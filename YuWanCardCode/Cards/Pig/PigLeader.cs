using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigLeader : YuWanCardModel
{
    public PigLeader() : base(
        baseCost: 2,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<PigLeaderPower>("BonusDamage", 2);
        WithTip(typeof(PigFriendsPower));
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["BonusDamage"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<PigLeaderPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["BonusDamage"].IntValue, Owner.Creature, this);
    }
}
