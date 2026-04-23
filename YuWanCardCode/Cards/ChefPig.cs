using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class ChefPig : YuWanCardModel
{
    public ChefPig() : base(
        baseCost: 2,
        type: CardType.Power,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<ChefPigPower>(1);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<ChefPigPower>(Owner.Creature, DynamicVars["ChefPigPower"].BaseValue, Owner.Creature, this);
    }
}
