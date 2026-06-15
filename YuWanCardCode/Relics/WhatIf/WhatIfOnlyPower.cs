using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfOnlyPower : WhatIfUniformCardRelicModel
{
    protected override bool Matches(CardModel card) => card.Type == CardType.Power;
}
