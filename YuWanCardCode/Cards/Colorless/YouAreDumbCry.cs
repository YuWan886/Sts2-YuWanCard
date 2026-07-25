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
        WithEnergy(1, 1);
        WithVar("CryCount", 1, 1);
        WithKeywords(CardKeyword.Exhaust);
        WithTip(typeof(PigAlwaysCry));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        int cryCount = DynamicVars["CryCount"].IntValue;
        List<CardPileAddResult> results = new();
        for (int i = 0; i < cryCount; i++)
        {
            var cryCard = CombatState!.CreateCard(ModelDb.Card<PigAlwaysCry>(), targetPlayer);
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(cryCard, PileType.Draw, Owner));
        }

        if (results.Count > 0)
        {
            CardCmd.PreviewCardPileAdd(results);
        }
    }
}
