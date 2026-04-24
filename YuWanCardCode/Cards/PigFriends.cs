using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigFriends : YuWanCardModel
{
    public PigFriends() : base(
        baseCost: 2,
        type: CardType.Power,
        rarity: CardRarity.Basic,
        target: TargetType.Self)
    {
        WithPower<PigFriendsPower>(1);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override PileType GetResultPileType()
    {
        return PileType.Discard;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<PigFriendsPower>(choiceContext, Owner.Creature, DynamicVars["PigFriendsPower"].IntValue, Owner.Creature, this);
    }
}
