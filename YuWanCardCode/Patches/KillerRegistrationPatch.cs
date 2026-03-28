using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using YuWanCard.Encounters;
using YuWanCard.Monsters;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class KillerRegistrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ModelDb.Contains(typeof(Killer)))
        {
            ModelDb.Inject(typeof(Killer));
        }
        if (!ModelDb.Contains(typeof(KillerElite)))
        {
            ModelDb.Inject(typeof(KillerElite));
        }
    }
}

[HarmonyPatch(typeof(Glory), nameof(Glory.GenerateAllEncounters))]
public class GloryKillerPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        var list = __result.ToList();
        var killerElite = ModelDb.Encounter<KillerElite>();
        if (killerElite != null && !list.Any(e => e is KillerElite))
        {
            list.Add(killerElite);
            __result = list;
        }
    }
}
