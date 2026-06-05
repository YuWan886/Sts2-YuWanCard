using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(SavedProperties))]
public static class SavedPropertySyncSuppressionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SavedProperties.FillInternal), [typeof(object)])]
    public static void BeforeFillInternal(object model)
    {
        SavedPropertyMultiplayerSync.BeginSavedPropertiesFill(model);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SavedProperties.FillInternal), [typeof(object)])]
    public static void AfterFillInternal(object model)
    {
        SavedPropertyMultiplayerSync.EndSavedPropertiesFill(model);
    }
}
