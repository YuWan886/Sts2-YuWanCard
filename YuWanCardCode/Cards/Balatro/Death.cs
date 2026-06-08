using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
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
        if (selectedCard.Owner == Owner && selectedCard.Pile?.Type == PileType.Hand)
        {
            return selectedCard;
        }

        if (NetCombatCardDb.Instance.TryGetCardId(selectedCard, out uint combatCardId)
            && NetCombatCardDb.Instance.TryGetCard(combatCardId, out CardModel? combatCard)
            && combatCard?.Owner == Owner
            && combatCard.Pile?.Type == PileType.Hand)
        {
            return combatCard;
        }

        SerializableCard serializedCard = selectedCard.ToSerializable();
        return PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(card =>
            card.IsMutable
            && card.Pile?.Type == PileType.Hand
            && card.ToSerializable().Equals(serializedCard)
            && card.EnergyCost?.GetResolved() == selectedCard.EnergyCost?.GetResolved());
    }
}
