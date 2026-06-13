using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class FoodComa : YuWanCardModel
{
    public FoodComa() : base(
        baseCost: 1,
        type: CardType.Power,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<FoodComaPower>("Block", 4);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<FoodComaPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["Block"].IntValue, Owner.Creature, this);
    }
}
