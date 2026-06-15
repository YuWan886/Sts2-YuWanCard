using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfOnlyColorless : WhatIfUniformCardRelicModel
{
    protected override bool Matches(CardModel card) =>
        card.VisualCardPool.IsColorless
        && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
}
