using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigAngry : YuWanCardModel
{
    public PigAngry() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies)
    {
        WithPower<StrengthPower>(4);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Strength.UpgradeValueBy(2m);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(CombatState!.GetTeammatesOf(Owner.Creature), DynamicVars.Strength.BaseValue, Owner.Creature, this);
    }
}
