using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfDiscovery : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Discovery>();

    public WhatIfDiscovery() : base(true)
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

        var discoveryModel = ModelDb.Card<Discovery>();

        var transformations = originalCards
            .Select(card => new CardTransformation(card, Owner.RunState.CreateCard(discoveryModel, Owner)))
            .ToList();

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner)
        {
            return false;
        }

        var discoveryModel = ModelDb.Card<Discovery>();

        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var discoveryCard = Owner!.RunState.CreateCard(discoveryModel, Owner);
            cardRewardOptions[i] = new CardCreationResult(discoveryCard);
        }

        return true;
    }
}
