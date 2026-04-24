using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class ManyPigs : YuWanCardModel
{
    protected override bool HasEnergyCostX => true;

    public ManyPigs() : base
    (
        baseCost: -1,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self
    )
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = ResolveEnergyXValue();
        if (IsUpgraded)
        {
            amount++;
        }
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<PigFriendsPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }
}
