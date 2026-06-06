using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigBirth : YuWanCardModel
{
    public PigBirth() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.None)
    {
        WithCards(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        var hand = PileType.Hand.GetPile(Owner);
        if (hand.Cards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(
                new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"),
                0,
                1
            );

            var cardsToDiscard = await CardSelectCmd.FromHandForDiscard(
                choiceContext,
                Owner,
                prefs,
                null,
                this
            );

            var discardList = cardsToDiscard.ToList();
            if (discardList.Count > 0)
            {
                await CardCmd.Discard(choiceContext, discardList);
            }
        }
    }
}
