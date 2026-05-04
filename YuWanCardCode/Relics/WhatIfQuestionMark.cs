using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfQuestionMark : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    private static readonly HashSet<MapPointType> EssentialTypes =
    [
        MapPointType.Boss,
        MapPointType.Ancient,
        MapPointType.RestSite
    ];

    public WhatIfQuestionMark() : base(true)
    {
    }
    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var map = Owner?.RunState?.Map;
        if (map == null) return;

        ForceMapToUnknown(map);
        RefreshNMapPoints(map);
    }

    public static void ForceMapToUnknown(ActMap map)
    {
        int changed = 0;
        foreach (var point in map.GetAllMapPoints())
        {
            if (!EssentialTypes.Contains(point.PointType))
            {
                point.PointType = MapPointType.Unknown;
                changed++;
            }
        }

        MainFile.Logger.Info(
            $"[WhatIfQuestionMark] ForceMapToUnknown: {changed} points changed to Unknown");
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
