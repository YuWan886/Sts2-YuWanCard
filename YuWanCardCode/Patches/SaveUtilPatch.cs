using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Patches;

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
