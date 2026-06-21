using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class AllIn : YuWanCardModel
{
    public AllIn() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithVar("Magic", 4);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Magic"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile == null)
            return;

        var allCards = discardPile.Cards.ToList();
        if (allCards.Count == 0)
            return;

        int count = DynamicVars["Magic"].IntValue;
        var cardsToPlay = DeterministicRandomUtils.TakeStableRandom(allCards, count, Owner.RunState.Rng.CombatCardSelection);

        foreach (var card in cardsToPlay)
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
        }

        foreach (var card in cardsToPlay)
        {
            Creature? target = GetTargetForCard(card);

            if (target == null && card.TargetType != TargetType.None && card.TargetType != TargetType.Self)
            {
                continue;
            }

            await card.OnPlayWrapper(choiceContext, target, isAutoPlay: true, new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = card.EnergyCost.GetAmountToSpend(),
                StarsSpent = 0,
                StarValue = 0
            }, skipCardPileVisuals: false);
        }
    }

    private Creature? GetTargetForCard(CardModel card)
    {
        return card.PickRandomTarget();
    }
}
