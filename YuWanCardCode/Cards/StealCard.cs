using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class StealCard : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public StealCard() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null || targetPlayer == Owner) return;

        var drawPile = PileType.Draw.GetPile(targetPlayer);
        var drawPileCards = drawPile.Cards.ToList();

        if (drawPileCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, drawPileCards, Owner, prefs);

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard != null)
        {
            int upgradeLevel = selectedCard.CurrentUpgradeLevel;
            
            await CardPileCmd.Add(selectedCard, PileType.Exhaust);
            
            var newCard = CombatState!.CreateCard(selectedCard.CanonicalInstance, Owner);
            for (int i = 0; i < upgradeLevel; i++)
            {
                CardCmd.Upgrade(newCard);
            }
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, addedByPlayer: true);
        }
    }
}
