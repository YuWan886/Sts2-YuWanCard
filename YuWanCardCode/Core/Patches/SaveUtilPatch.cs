using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(SaveUtil), nameof(SaveUtil.EncounterOrDeprecated))]
public static class SaveUtilPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ModelId id, ref EncounterModel __result)
    {
        if (id == null || string.IsNullOrEmpty(id.Entry))
        {
            MainFile.Logger.Warn($"EncounterOrDeprecated called with null id, returning DeprecatedEncounter");
            __result = ModelDb.Encounter<DeprecatedEncounter>();
            return false;
        }
        return true;
    }
}

/// Excluded from auto-discovery on all platforms — applied manually in MainFile.cs
/// (ref ModelId parameters fail on Android/Mono AOT so skipped there entirely).
[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.GetOrCreateEncounterStats))]
public static class ProgressStateEncounterStatsPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref ModelId encounterId)
    {
        if (encounterId == null || string.IsNullOrEmpty(encounterId.Entry))
        {
            MainFile.Logger.Warn("GetOrCreateEncounterStats called with null encounter id, falling back to DeprecatedEncounter");
            encounterId = ModelDb.Encounter<DeprecatedEncounter>().Id;
        }
    }
}
