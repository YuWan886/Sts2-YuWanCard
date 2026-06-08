using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Death : YuWanCardModel
{
    public Death() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectable = BalatroCardEditionHelper.GetSelectableHandCards(Owner, this)
            .Where(card => card.DeckVersion != null || card.Pile?.Type == PileType.Deck)
            .ToList();
        if (selectable.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this && (card.DeckVersion != null || card.Pile?.Type == PileType.Deck),
            source: this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        CardModel? resolved = ResolveSelectedHandCard(selected);
        if (resolved == null)
        {
            return;
        }

        CardModel deckCard = resolved.DeckVersion ?? selected.DeckVersion ?? resolved;
        await CardPileCmd.RemoveFromCombat(resolved);
        await CardPileCmd.RemoveFromDeck(deckCard);
        await PlayerCmd.GainGold(IsUpgraded ? 25 : 15, Owner);
    }

    private CardModel? ResolveSelectedHandCard(CardModel selectedCard)
    {
        return CombatCardStateHelper.ResolveSelectedHandCard(Owner, selectedCard);
    }
}
