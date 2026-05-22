using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfDirectFlight : WhatIfRelicModel
{
    public WhatIfDirectFlight() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        TryEnableTravelMode(Owner?.RunState, NMapScreen.Instance);
    }

    public static bool HasDirectFlight(IRunState? runState)
    {
        return runState?.Players.Any(player => player.Relics.Any(relic => relic is WhatIfDirectFlight)) == true;
    }

    public static bool HasVisited(IRunState? runState, MapCoord coord)
    {
        return runState is RunState state && state.VisitedMapCoords.Contains(coord);
    }

    public static void TryEnableTravelMode(IRunState? runState, NMapScreen? mapScreen)
    {
        if (!HasDirectFlight(runState) || mapScreen == null || mapScreen.IsDebugTravelEnabled)
        {
            return;
        }

        mapScreen.SetDebugTravelEnabled(true);
        MainFile.Logger.Info("[WhatIfDirectFlight] Travel mode enabled");
    }
}
