using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class KillingIntent : YuWanCardModel
{
    public KillingIntent() : base(
        baseCost: 0,
        type: CardType.Power,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<KillingIntentPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["KillingIntentPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KillingIntentPower>(Owner.Creature, DynamicVars["KillingIntentPower"].BaseValue, Owner.Creature, this);
    }
}
