using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMapScreen))]
[HarmonyPatch("RecalculateTravelability")]
public static class MapScreenPatch
{
    [HarmonyPostfix]
    public static void RecalculateTravelabilityPostfix(NMapScreen __instance)
    {
        var runStateField = __instance.GetType().GetField("_runState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (runStateField?.GetValue(__instance) is not RunState runState)
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

        var mapField = __instance.GetType().GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (mapField?.GetValue(__instance) is not ActMap map)
        {
            return;
        }

        var mapPointDictionaryField = __instance.GetType().GetField("_mapPointDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (mapPointDictionaryField?.GetValue(__instance) is not IDictionary<MegaCrit.Sts2.Core.Map.MapCoord, NMapPoint> mapPointDictionary || !runState.VisitedMapCoords.Any())
        {
            return;
        }

        var visitedMapCoords = runState.VisitedMapCoords;
        var lastVisitedCoord = visitedMapCoords[visitedMapCoords.Count - 1];

        if (lastVisitedCoord.row >= map.GetRowCount() - 1)
        {
            return;
        }

        var secondBossPointNodeField = __instance.GetType().GetField("_secondBossPointNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bossPointNodeField = __instance.GetType().GetField("_bossPointNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bossPointNode = bossPointNodeField?.GetValue(__instance) as NMapPoint;

        if (secondBossPointNodeField?.GetValue(__instance) is NMapPoint secondBossPointNode && lastVisitedCoord == bossPointNode?.Point.coord)
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
