using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfAllForms : YuWanRelicModel
{
    private static readonly CardModel[] FormCards =
    [
        ModelDb.Card<DemonForm>(),
        ModelDb.Card<EchoForm>(),
        ModelDb.Card<SerpentForm>(),
        ModelDb.Card<ReaperForm>(),
        ModelDb.Card<VoidForm>(),
        ModelDb.Card<WraithForm>()
    ];

    public override RelicRarity Rarity => RelicRarity.Event;

    public WhatIfAllForms() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        var cardsToAdd = FormCards
            .Select(card => Owner.RunState.CreateCard(card, Owner))
            .ToList();

        await CardPileCmd.Add(cardsToAdd, PileType.Deck);
    }
}
