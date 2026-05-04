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
public class WhatIfWhistle : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Whistle>();

    public WhatIfWhistle() : base(true)
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

        var whistleModel = ModelDb.Card<Whistle>();

        var transformations = originalCards.Select(card =>
            new CardTransformation(card, Owner.RunState.CreateCard(whistleModel, Owner)));

        var results = await CardCmd.Transform(transformations, null, CardPreviewStyle.None);

    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner)
        {
            return false;
        }

        var whistleModel = ModelDb.Card<Whistle>();
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var whistleCard = Owner.RunState.CreateCard(whistleModel, Owner);
            cardRewardOptions[i] = new CardCreationResult(whistleCard);
        }

        return true;
    }
}
