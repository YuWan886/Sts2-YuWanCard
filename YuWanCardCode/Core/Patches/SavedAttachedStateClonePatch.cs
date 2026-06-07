using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Persistence;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(AbstractModel))]
public static class SavedAttachedStateClonePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(AbstractModel.MutableClone))]
    public static void AfterMutableClone(AbstractModel __instance, AbstractModel __result)
    {
        if (__result == null || ReferenceEquals(__instance, __result))
        {
            return;
        }

        SavedAttachedStateRegistry.CloneAttachedStates(__instance, __result);
    }
}
