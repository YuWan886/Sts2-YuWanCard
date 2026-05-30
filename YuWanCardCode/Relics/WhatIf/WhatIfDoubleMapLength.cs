using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfDoubleMapLength : WhatIfRelicModel
{
    public WhatIfDoubleMapLength() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await RunManager.Instance.GenerateMap();
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (runState.Players.Any(player => player.Relics.Any(relic => relic is WhatIfDoubleMapLength)))
        {
            var doubled = new DoubleLengthActMap(map);
            RefreshNMapPoints(doubled);
            return doubled;
        }

        return map;
    }

    private static void RefreshNMapPoints(ActMap map)
    {
        var screen = NMapScreen.Instance;
        if (screen == null) return;

        var dict = YuWanReflectionHelper
            .GetPrivateField<IDictionary<MapCoord, NMapPoint>>(screen, "_mapPointDictionary");
        if (dict == null) return;

        foreach (var point in map.GetAllMapPoints())
        {
            if (dict.TryGetValue(point.coord, out var nPoint))
                nPoint.RefreshVisualsInstantly();
        }
    }
}
