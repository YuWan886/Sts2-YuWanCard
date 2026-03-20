using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigKing : YuWanCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public PigKing() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var transformableCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.IsTransformable)
            .ToList();

        List<CardModel> cardList;
        if (transformableCards.Count <= 3)
        {
            cardList = transformableCards;
        }
        else
        {
            var selectedCards = await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 3, 3),
                context: choiceContext,
                player: Owner,
                filter: c => c.IsTransformable,
                source: this
            );
            cardList = selectedCards.ToList();
        }

        var transformations = new List<CardTransformation>();
        
        if (cardList.Count > 0 && CombatState != null)
        {
            var card1 = CombatState.CreateCard<PigCharge>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(card1);
            transformations.Add(new CardTransformation(cardList[0], card1));
        }
        
        if (cardList.Count > 1 && CombatState != null)
        {
            var card2 = CombatState.CreateCard<PigMultiShot>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(card2);
            transformations.Add(new CardTransformation(cardList[1], card2));
        }
        
        if (cardList.Count > 2 && CombatState != null)
        {
            var card3 = CombatState.CreateCard<PigShieldBreak>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(card3);
            transformations.Add(new CardTransformation(cardList[2], card3));
        }

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, Owner.RunState.Rng.CombatCardGeneration);
        }
    }
}
