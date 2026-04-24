using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigMakeWish : YuWanCardModel
{
    private static readonly LocString SelectionPrompt = new("cards", "YUWANCARD-PIG_MAKE_WISH.selectionScreenPrompt");

    public PigMakeWish() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null) return;

        int cardCount = IsUpgraded ? 8 : 6;
        int maxSelect = IsUpgraded ? 3 : 2;

        var allCards = PigCardPoolUtils.GetAllUnlockedCards(Owner);
        if (allCards.Count == 0) return;

        var shuffled = allCards.ToList().UnstableShuffle(Owner.RunState.Rng.CombatCardGeneration);
        var cardsToOffer = shuffled.Take(Math.Min(cardCount, shuffled.Count)).ToList();

        var cardCreationResults = cardsToOffer.Select(c => new CardCreationResult(
            CombatState.CreateCard(c, Owner)
        )).ToList();

        var prefs = new CardSelectorPrefs(SelectionPrompt, 0, maxSelect);

        var selectedCards = await CardSelectCmd.FromSimpleGridForRewards(
            prefs: prefs,
            context: choiceContext,
            cards: cardCreationResults,
            player: Owner
        );

        if (selectedCards.Any())
        {
            await CardPileCmd.AddGeneratedCardsToCombat(selectedCards.ToList(), PileType.Hand, Owner);
        }
    }
}
