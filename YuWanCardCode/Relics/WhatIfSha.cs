using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Cards;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfSha : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Sha>();

    public WhatIfSha() : base(true)
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

        var shaModel = ModelDb.Card<Sha>();

        var transformations = originalCards.Select(card =>
            new CardTransformation(card, Owner.RunState.CreateCard(shaModel, Owner)));

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner)
        {
            return false;
        }

        var shaModel = ModelDb.Card<Sha>();
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var shaCard = Owner.RunState.CreateCard(shaModel, Owner);
            cardRewardOptions[i] = new CardCreationResult(shaCard);
        }

        return true;
    }
}
