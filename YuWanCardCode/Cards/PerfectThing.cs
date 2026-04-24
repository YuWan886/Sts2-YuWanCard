using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PerfectThing : YuWanCardModel
{
    public PerfectThing() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Ancient,
        target: TargetType.Self)
    {
        WithPower<PerfectThingPower>(1);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var power = await PowerCmd.Apply<PerfectThingPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);

        if (power != null && IsUpgraded)
        {
            if (power is PerfectThingPower perfectThingPower)
            {
                perfectThingPower.SetCardsPerEnergy(2);
            }
        }
    }
}
