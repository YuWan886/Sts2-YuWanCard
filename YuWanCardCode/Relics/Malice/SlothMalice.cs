using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class SlothMalice : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public SlothMalice() : base(true)
    {
    }
}
