using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class Wyjk : YuWanCardModel
{
    public Wyjk() : base(
        baseCost: 2,
        type: CardType.Power,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies)
    {
        WithVars(new EnergyVar(2));
        WithEnergyTip();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);
        foreach (var teammate in teammates)
        {
            await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, teammate.Player!);
        }
    }
}
