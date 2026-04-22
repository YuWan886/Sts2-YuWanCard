using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMapScreen))]
[HarmonyPatch("RecalculateTravelability")]
public static class MapScreenPatch
{
    [HarmonyPostfix]
    public static void RecalculateTravelabilityPostfix(NMapScreen __instance)
    {
        var runState = YuWanReflectionHelper.GetPrivateField<RunState>(__instance, "_runState");
        if (runState == null)
        {
            return;
        }

        bool hasRingOfSevenCurses = runState.Players.Any(p =>
            p.Relics.Any(r => r is RingOfSevenCurses)
        );

        if (!hasRingOfSevenCurses)
        {
            return;
        }

        bool hasFlightModifier = runState.Modifiers.OfType<MegaCrit.Sts2.Core.Models.Modifiers.Flight>().Any();
        if (hasFlightModifier)
        {
            return;
        }

        var map = YuWanReflectionHelper.GetPrivateField<ActMap>(__instance, "_map");
        if (map == null)
        {
            return;
        }

        var mapPointDictionary = YuWanReflectionHelper.GetPrivateField<IDictionary<MegaCrit.Sts2.Core.Map.MapCoord, NMapPoint>>(__instance, "_mapPointDictionary");
        if (mapPointDictionary == null || !runState.VisitedMapCoords.Any())
        {
            return;
        }

        var visitedMapCoords = runState.VisitedMapCoords;
        var lastVisitedCoord = visitedMapCoords[visitedMapCoords.Count - 1];

        if (lastVisitedCoord.row >= map.GetRowCount() - 1)
        {
            return;
        }

        var secondBossPointNode = YuWanReflectionHelper.GetPrivateField<NMapPoint>(__instance, "_secondBossPointNode");
        var bossPointNode = YuWanReflectionHelper.GetPrivateField<NMapPoint>(__instance, "_bossPointNode");

        if (secondBossPointNode != null && lastVisitedCoord == bossPointNode?.Point.coord)
        {
            secondBossPointNode.State = MapPointState.Travelable;
            return;
        }

        var nextRowPoints = map.GetPointsInRow(lastVisitedCoord.row + 1);
        foreach (var point in nextRowPoints)
        {
            if (mapPointDictionary.TryGetValue(point.coord, out var mapPoint))
            {
                mapPoint.State = MapPointState.Travelable;
            }
        }
    }
}
