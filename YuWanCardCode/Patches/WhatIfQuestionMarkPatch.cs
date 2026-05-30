using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

/// <summary>
/// Transforms all map points (except Ancient and Boss) to Unknown when any
/// player has the WhatIfQuestionMark relic.
/// </summary>
[HarmonyPatch(typeof(NMapScreen))]
[HarmonyPatch("SetMap")]
public static class WhatIfQuestionMarkMapPatch
{
    [HarmonyPrefix]
    public static void Prefix(ActMap map)
    {
        var state = RunManager.Instance?.State;
        if (state == null || map == null)
            return;

        var hasRelic = state.Players.Any(p =>
            p.Relics.Any(r => r is WhatIfQuestionMark));

        if (hasRelic)
            WhatIfQuestionMark.ForceMapToUnknown(map);
    }
}
