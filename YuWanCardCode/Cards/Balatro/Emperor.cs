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
public sealed class Emperor : YuWanCardModel
{
    public Emperor() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectable = BalatroCardEditionHelper.GetSelectableHandCards(Owner, this).ToList();
        if (selectable.Count == 0)
        {
            return;
        }

        int picks = IsUpgraded ? Math.Min(2, selectable.Count) : 1;
        var selected = await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), picks, picks),
            context: choiceContext,
            player: Owner,
            filter: card => card != this,
            source: this);

        foreach (CardModel card in selected)
        {
            card.EnergyCost.SetCustomBaseCost(0);
            card.DeckVersion?.EnergyCost.SetCustomBaseCost(0);
        }
    }
}
