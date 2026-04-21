using BaseLib.Patches.Hooks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class BraveRib : YuWanCardModel
{
    public BraveRib() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {   
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        int maxHandSize = MaxHandSizePatch.GetMaxHandSize(Owner);

        var cardsToExhaust = hand.Cards.Where(c => c != this).ToList();
        foreach (var card in cardsToExhaust)
        {
            await CardPileCmd.Add(card, PileType.Exhaust);
        }

        int cardsToDraw = maxHandSize - 1;
        if (cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);
        }
    }
}
