using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
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
            MainFile.Logger.Info("Killer monster registered to ModelDb");
        }
        if (!ModelDb.Contains(typeof(KillerElite)))
        {
            ModelDb.Inject(typeof(KillerElite));
            MainFile.Logger.Info("KillerElite encounter registered to ModelDb");
        }
    }
}

[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
public class OvergrowthKillerPatch
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
            MainFile.Logger.Info("KillerElite added to Overgrowth encounters");
        }
    }
}

[HarmonyPatch(typeof(Hive), nameof(Hive.GenerateAllEncounters))]
public class HiveKillerPatch
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
            MainFile.Logger.Info("KillerElite added to Hive encounters");
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
            MainFile.Logger.Info("KillerElite added to Glory encounters");
        }
    }
}
