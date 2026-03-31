using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class StoneCarryingKing : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    public StoneCarryingKing() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithTip(typeof(GiantRock));
    }

    public override void OnUpgrade()
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        var targetHand = PileType.Hand.GetPile(targetPlayer);
        var handCards = targetHand.Cards.ToList();

        if (handCards.Count == 0 || CombatState == null) return;

        var transformations = new List<CardTransformation>();

        foreach (var card in handCards)
        {
            var giantRock = CombatState.CreateCard<GiantRock>(targetPlayer);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(giantRock);
            }
            transformations.Add(new CardTransformation(card, giantRock));
        }

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, Owner.RunState.Rng.CombatCardGeneration);
        }
    }
}
