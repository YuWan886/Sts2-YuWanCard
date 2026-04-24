using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigBurger : YuWanCardModel
{
    public PigBurger() : base(
        baseCost: 1,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<PigBurgerPower>(6);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PigBurgerPower"].UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<PigBurgerPower>(choiceContext, Owner.Creature, DynamicVars["PigBurgerPower"].BaseValue, Owner.Creature, this);
    }
}
