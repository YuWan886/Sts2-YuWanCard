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
public sealed class Priestess : YuWanCardModel
{
    public Priestess() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var options = BalatroCardEditionHelper.GetSelectableHandCards(Owner, this).ToList();
        if (options.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this,
            source: this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        int count = IsUpgraded ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            CardModel copy = CardCopyHelper.CreateCopy(selected, Owner);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner);
        }
    }
}
