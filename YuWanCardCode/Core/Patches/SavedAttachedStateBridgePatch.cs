using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Persistence;

namespace YuWanCard.Core.Patches;

public static class SavedAttachedStateBridgePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SavedProperties), nameof(SavedProperties.FromInternal), [typeof(object), typeof(ModelId)])]
    public static void AfterSavedPropertiesFromInternal(ref SavedProperties? __result, object model)
    {
        SavedAttachedStateRegistry.ExportAttachedStates(ref __result, model);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SavedProperties), nameof(SavedProperties.FillInternal), [typeof(object)])]
    public static void AfterSavedPropertiesFillInternal(SavedProperties __instance, object model)
    {
        SavedAttachedStateRegistry.ImportAttachedStates(__instance, model);
    }
}
