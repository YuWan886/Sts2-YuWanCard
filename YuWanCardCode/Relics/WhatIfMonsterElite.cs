using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfMonsterElite : WhatIfRelicModel
{
    public WhatIfMonsterElite() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var map = Owner?.RunState?.Map;
        if (map == null)
        {
            return;
        }

        ForceMonsterRoomsToElite(map);
        RefreshNMapPoints(map);
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (runState.Players.Any(player => player.Relics.Any(relic => relic is WhatIfMonsterElite)))
        {
            ForceMonsterRoomsToElite(map);
        }

        return map;
    }

    public static void ForceMonsterRoomsToElite(ActMap map)
    {
        foreach (var point in map.GetAllMapPoints())
        {
            if (point.PointType == MapPointType.Monster)
            {
                point.PointType = MapPointType.Elite;
            }
        }
    }

    private static void RefreshNMapPoints(ActMap map)
    {
        var screen = NMapScreen.Instance;
        if (screen == null)
        {
            return;
        }

        var dict = YuWanReflectionHelper
            .GetPrivateField<IDictionary<MapCoord, NMapPoint>>(screen, "_mapPointDictionary");
        if (dict == null)
        {
            return;
        }

        foreach (var point in map.GetAllMapPoints())
        {
            if (dict.TryGetValue(point.coord, out var nPoint))
            {
                nPoint.RefreshVisualsInstantly();
            }
        }
    }
}
