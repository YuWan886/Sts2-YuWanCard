using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfJackpot : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Jackpot>();

    public WhatIfJackpot() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Deck == null)
        {
            return;
        }

        var originalCards = Owner.Deck.Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (originalCards.Count == 0)
        {
            return;
        }

        var jackpotModel = ModelDb.Card<Jackpot>();

        var transformations = originalCards.Select(card =>
            new CardTransformation(card, Owner.RunState.CreateCard(jackpotModel, Owner)));

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner)
        {
            return false;
        }

        var jackpotModel = ModelDb.Card<Jackpot>();
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var jackpotCard = Owner.RunState.CreateCard(jackpotModel, Owner);
            cardRewardOptions[i] = new CardCreationResult(jackpotCard);
        }

        return true;
    }
}
