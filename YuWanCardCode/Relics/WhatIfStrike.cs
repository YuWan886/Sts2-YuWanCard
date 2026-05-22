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
public class WhatIfStrike : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Hellraiser>();

    public WhatIfStrike() : base(true)
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

        var hellraiserModel = ModelDb.Card<Hellraiser>();

        var strikeCards = ModelDb.AllCards
            .Where(c => c.Tags.Contains(CardTag.Strike))
            .ToList();

        if (strikeCards.Count == 0)
        {
            return;
        }

        var transformations = new List<CardTransformation>();

        for (int i = 0; i < originalCards.Count; i++)
        {
            if (i == 0)
            {
                transformations.Add(new CardTransformation(originalCards[i], Owner.RunState.CreateCard(hellraiserModel, Owner)));
            }
            else
            {
                var randomStrike = strikeCards[Owner.RunState.Rng.Niche.NextInt(strikeCards.Count)];
                transformations.Add(new CardTransformation(originalCards[i], Owner.RunState.CreateCard(randomStrike, Owner)));
            }
        }

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner)
        {
            return false;
        }

        var strikeCards = ModelDb.AllCards
            .Where(c => c.Tags.Contains(CardTag.Strike))
            .ToList();

        if (strikeCards.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var randomStrike = strikeCards[Owner!.RunState.Rng.Niche.NextInt(strikeCards.Count)];
            var strikeCard = Owner.RunState.CreateCard(randomStrike, Owner);
            cardRewardOptions[i] = new CardCreationResult(strikeCard);
        }

        return true;
    }
}
