using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relic;

namespace YuWanCard.Patches;

/// <summary>
/// 修复地图选择逻辑，使七咒之戒拥有飞行效果
/// </summary>
[HarmonyPatch(typeof(NMapScreen))]
[HarmonyPatch("RecalculateTravelability")]
public static class MapScreenPatch
{
    /// <summary>
    /// 修改地图节点的可达性计算逻辑
    /// 当有任何玩家拥有七咒之戒时，允许选择下一行的所有节点（飞行效果）
    /// </summary>
    [HarmonyPrefix]
    public static bool RecalculateTravelabilityPrefix(NMapScreen __instance)
    {
        var runStateField = __instance.GetType().GetField("_runState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (runStateField?.GetValue(__instance) is not RunState runState)
        {
            return true;
        }

        // 检查是否有任何玩家拥有七咒之戒
        bool hasRingOfSevenCurses = runState.Players.Any(p =>
            p.Relics.Any(r => r is RingOfSevenCurses)
        );

        if (!hasRingOfSevenCurses)
        {
            return true;
        }

        // 如果有七咒之戒，修改飞行逻辑
        var mapField = __instance.GetType().GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (mapField?.GetValue(__instance) is not ActMap map)
        {
            return true;
        }

        // 检查是否已经有飞行特效
        bool hasFlightModifier = runState.Modifiers.OfType<MegaCrit.Sts2.Core.Models.Modifiers.Flight>().Any();
        if (hasFlightModifier)
        {
            return true; // 已经有飞行特效，不需要额外处理
        }

        // 临时添加飞行特效到检查中
        var mapPointDictionaryField = __instance.GetType().GetField("_mapPointDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (mapPointDictionaryField?.GetValue(__instance) is not IDictionary<MegaCrit.Sts2.Core.Map.MapCoord, NMapPoint> mapPointDictionary || !runState.VisitedMapCoords.Any())
        {
            return true;
        }

        // 设置所有已访问节点为已旅行状态
        foreach (var value in mapPointDictionary.Values)
        {
            value.State = MapPointState.Untravelable;
        }

        foreach (var visitedMapCoord in runState.VisitedMapCoords)
        {
            mapPointDictionary[visitedMapCoord].State = MapPointState.Traveled;
        }

        var visitedMapCoords = runState.VisitedMapCoords;
        var mapCoord = visitedMapCoords[visitedMapCoords.Count - 1];

        // 检查是否有第二个 BOSS 点
        var secondBossPointNodeField = __instance.GetType().GetField("_secondBossPointNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bossPointNodeField = __instance.GetType().GetField("_bossPointNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bossPointNode = bossPointNodeField?.GetValue(__instance) as NMapPoint;

        if (secondBossPointNodeField?.GetValue(__instance) is NMapPoint secondBossPointNode && mapCoord == bossPointNode?.Point.coord)
        {
            secondBossPointNode.State = MapPointState.Travelable;
            return false;
        }

        if (mapCoord.row != map.GetRowCount() - 1)
        {
            // 使用飞行逻辑：获取下一行的所有节点
            var enumerable = map.GetPointsInRow(mapCoord.row + 1);
            foreach (var item in enumerable)
            {
                mapPointDictionary[item.coord].State = MapPointState.Travelable;
            }
            return false;
        }

        if (bossPointNode != null)
        {
            bossPointNode.State = MapPointState.Travelable;
        }
        return false;
    }
}
