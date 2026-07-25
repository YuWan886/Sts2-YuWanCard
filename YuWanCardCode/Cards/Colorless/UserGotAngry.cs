using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class UserGotAngry : YuWanCardModel
{
    public UserGotAngry() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithTip(card => HoverTipFactory.FromCard<Anger>(card.IsUpgraded));
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        List<CardPileAddResult> addResults = [];
        foreach (PileType pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust })
        {
            var anger = CombatState.CreateCard<Anger>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(anger);
            addResults.Add(await CardPileCmd.AddGeneratedCardToCombat(anger, pileType, Owner));
        }

        CardCmd.PreviewCardPileAdd(addResults, 2f);
    }
}
