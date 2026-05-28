using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
public static class WhatIfDirectFlightMapScreenPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        var runState = YuWanReflectionHelper.GetPrivateField<RunState>(__instance, "_runState");
        WhatIfDirectFlight.TryEnableTravelMode(runState, __instance);
    }
}

[HarmonyPatch(typeof(NMapPoint), "get_IsTravelable")]
public static class WhatIfDirectFlightMapPointPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapPoint __instance, ref bool __result)
    {
        if (!__result)
            return;

        var runState = YuWanReflectionHelper.GetPrivateField<IRunState>(__instance, "_runState");
        if (!WhatIfDirectFlight.HasDirectFlight(runState))
            return;

        if (WhatIfDirectFlight.HasVisited(runState, __instance.Point.coord))
            __result = false;
    }
}
