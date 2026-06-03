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
public sealed class Obsidian : YuWanCardModel
{
    public Obsidian() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectable = BalatroCardEditionHelper.GetSelectableHandCards(Owner, this)
            .Where(card => BalatroCardEditionHelper.CanApplyEdition(card, BalatroCardEdition.Polychrome))
            .ToList();
        if (selectable.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this && BalatroCardEditionHelper.CanApplyEdition(card, BalatroCardEdition.Polychrome),
            source: this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        if (!BalatroCardEditionHelper.TryApplyEdition(selected, BalatroCardEdition.Polychrome))
        {
            return;
        }

        if (!IsUpgraded)
        {
            List<CardModel> removable = Owner.Deck.Cards
                .Where(card => card != selected.DeckVersion)
                .ToList();
            if (removable.Count > 0)
            {
                CardModel removed = removable[Owner.RunState.Rng.Niche.NextInt(removable.Count)];
                await CardPileCmd.RemoveFromDeck(removed, showPreview: false);
            }
        }
    }
}
