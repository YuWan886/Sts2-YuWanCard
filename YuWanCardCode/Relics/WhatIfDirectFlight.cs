using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfDirectFlight : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public WhatIfDirectFlight() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var mapScreen = NMapScreen.Instance;
        if (mapScreen != null && !mapScreen.IsDebugTravelEnabled)
        {
            mapScreen.SetDebugTravelEnabled(true);
            MainFile.Logger.Info("[WhatIfDirectFlight] Travel mode enabled");
        }
    }
}
