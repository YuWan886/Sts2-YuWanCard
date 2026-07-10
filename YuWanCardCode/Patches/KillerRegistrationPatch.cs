using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using YuWanCard.Config;
using YuWanCard.Encounters;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Glory), nameof(Glory.GenerateAllEncounters))]
public class GloryEliteEncounterPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        var list = __result.ToList();
        AddEncounterIfEnabled<KillerElite>(list);
        AddEncounterIfEnabled<FerrousWroughtnautElite>(list);
        __result = list;
    }

    private static void AddEncounterIfEnabled<TEncounter>(List<EncounterModel> encounters)
        where TEncounter : EncounterModel
    {
        if (!YuWanContentAvailability.IsEncounterTypeEnabled<TEncounter>())
        {
            return;
        }

        var encounter = ModelDb.Encounter<TEncounter>();
        if (encounter != null && !encounters.Any(existing => existing is TEncounter))
        {
            encounters.Add(encounter);
        }
    }
}
