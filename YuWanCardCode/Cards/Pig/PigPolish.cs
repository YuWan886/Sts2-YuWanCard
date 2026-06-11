using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPolish : YuWanCardModel
{
    public PigPolish() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithCards(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this && card.IsUpgradable,
            source: this)).FirstOrDefault();
        if (selected != null)
        {
            CardCmd.Upgrade(selected);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
