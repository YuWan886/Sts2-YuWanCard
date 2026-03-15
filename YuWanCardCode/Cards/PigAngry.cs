using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigAngry : YuWanCardModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(4m)];

    public PigAngry() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(CombatState!.GetTeammatesOf(Owner.Creature), DynamicVars.Strength.BaseValue, Owner.Creature, this);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Strength.UpgradeValueBy(2m);
    }
}
