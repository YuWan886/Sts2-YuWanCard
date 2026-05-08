using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigMakeFood : YuWanCardModel
{
    public PigMakeFood() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVar("ExhaustCount", 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExhaustCount"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        var handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this)
            .ToList();

        if (handCards.Count == 0) return;

        int maxExhaust = Math.Min(DynamicVars["ExhaustCount"].IntValue, handCards.Count);

        var prefs = new CardSelectorPrefs(
            new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"),
            1,
            maxExhaust
        );

        var selectedCards = await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: prefs,
            filter: c => c != this,
            source: this
        );

        var cardsToExhaust = selectedCards.ToList();

        foreach (var card in cardsToExhaust)
        {
            await CardPileCmd.Add(card, PileType.Exhaust);
        }

        int foodCount = cardsToExhaust.Count;

        for (int i = 0; i < foodCount; i++)
        {
            var canonicalCard = CardUtils.GetRandomFoodPigCardCanonical(Owner);
            var foodCard = CombatState.CreateCard(canonicalCard, Owner);
            await CardPileCmd.AddGeneratedCardToCombat(foodCard, PileType.Hand, cardPlay.Card.Owner);
        }
    }
}
