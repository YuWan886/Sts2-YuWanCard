using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigThink : YuWanCardModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip];

    public override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    public PigThink() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<Creature> teammates = from c in CombatState!.GetTeammatesOf(Owner.Creature)
                                          where c != null && c.IsAlive && c.IsPlayer
                                          select c;
        foreach (Creature teammate in teammates)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, teammate.Player!);
        }
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
