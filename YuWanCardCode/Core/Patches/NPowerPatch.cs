using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NPower))]
public static class NPowerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnHovered")]
    private static bool Prefix_OnHovered(NPower __instance)
    {
        var model = __instance.Model;
        if (model?.Owner == null)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ShowPowerHoverTips")]
    private static bool Prefix_ShowPowerHoverTips(NPower __instance)
    {
        var model = __instance.Model;
        if (model?.Owner == null)
        {
            return false;
        }
        return true;
    }
}
