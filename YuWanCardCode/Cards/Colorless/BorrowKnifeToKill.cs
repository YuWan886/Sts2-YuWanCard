using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class BorrowKnifeToKill : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public BorrowKnifeToKill() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targetPlayer = cardPlay.Target?.Player;
        if (targetPlayer == null || targetPlayer == Owner) return;

        var handCards = PileType.Hand.GetPile(targetPlayer).Cards
            .Where(c => c.Type == CardType.Attack && !c.EnergyCost.CostsX)
            .ToList();

        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, handCards, Owner, prefs);

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null) return;

        Creature? target = GetTargetForCard(selectedCard);
        if (target == null && selectedCard.TargetType != TargetType.None && selectedCard.TargetType != TargetType.Self)
        {
            return;
        }

        await selectedCard.OnPlayWrapper(choiceContext, target, isAutoPlay: true, new ResourceInfo
        {
            EnergySpent = 0,
            EnergyValue = 0,
            StarsSpent = 0,
            StarValue = 0
        });
    }

    private Creature? GetTargetForCard(CardModel card)
    {
        return card.PickRandomTarget();
    }
}
