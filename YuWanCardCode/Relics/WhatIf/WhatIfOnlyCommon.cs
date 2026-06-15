using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfOnlyCommon : WhatIfUniformCardRelicModel
{
    protected override bool Matches(CardModel card) => card.Rarity == CardRarity.Common;
}
