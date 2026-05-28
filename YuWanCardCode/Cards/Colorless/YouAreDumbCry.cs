using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class YouAreDumbCry : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public YouAreDumbCry() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithVars(new EnergyVar(1));
        WithVar("CryCount", 1);
        WithKeywords(CardKeyword.Exhaust);
        WithEnergyTip();
        WithTip(typeof(PigAlwaysCry));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars["CryCount"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        int cryCount = DynamicVars["CryCount"].IntValue;
        for (int i = 0; i < cryCount; i++)
        {
            var cryCard = CombatState!.CreateCard(ModelDb.Card<PigAlwaysCry>(), targetPlayer);
            await CardPileCmd.AddGeneratedCardToCombat(cryCard, PileType.Draw, addedByPlayer: true);
        }
    }
}
