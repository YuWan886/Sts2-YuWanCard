using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ProgressSaveManager), "IncrementEncounterLoss")]
public static class ProgressSaveManagerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ModelId encounterId)
    {
        if (encounterId == null || string.IsNullOrEmpty(encounterId.Entry))
        {
            MainFile.Logger.Warn($"Skipped IncrementEncounterLoss with null encounterId");
            return false;
        }
        return true;
    }
}
