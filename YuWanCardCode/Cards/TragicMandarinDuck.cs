using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class TragicMandarinDuck : YuWanCardModel
{
    public TragicMandarinDuck() : base(
        baseCost: 0,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<StrengthPower>(2);
        WithPower<DexterityPower>(2);
        WithVar("Damage", 6);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1);
        DynamicVars.Dexterity.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TragicMandarinDuckPower>(Owner.Creature, 1, Owner.Creature, this);
    }
}
