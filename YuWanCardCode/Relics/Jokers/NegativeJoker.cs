using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class NegativeJoker : YuWanJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
}
