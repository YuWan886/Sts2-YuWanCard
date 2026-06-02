using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfInfiniteUpgrades : WhatIfRelicModel
{
    public WhatIfInfiniteUpgrades() : base(true)
    {
    }
}
