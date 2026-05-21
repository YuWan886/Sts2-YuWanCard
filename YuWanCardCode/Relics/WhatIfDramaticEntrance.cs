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
public class WhatIfDramaticEntrance : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<DramaticEntrance>();

    public WhatIfDramaticEntrance() : base(true)
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

        var dramaticEntranceModel = ModelDb.Card<DramaticEntrance>();
        var transformations = originalCards
            .Select(card => new CardTransformation(card, Owner.RunState.CreateCard(dramaticEntranceModel, Owner)))
            .ToList();

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        var dramaticEntranceModel = ModelDb.Card<DramaticEntrance>();
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            cardRewardOptions[i] = new CardCreationResult(Owner.RunState.CreateCard(dramaticEntranceModel, Owner));
        }

        return true;
    }
}
