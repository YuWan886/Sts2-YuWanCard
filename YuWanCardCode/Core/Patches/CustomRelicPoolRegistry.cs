using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Registers custom relic pools with the game. Custom relic pool types
/// (extending RelicPoolModel + IYuWanContent) are auto-detected during
/// ContentRegistry scanning and registered here during ModelDb.Init.
/// </summary>
public static class CustomRelicPoolRegistry
{
    public static readonly List<RelicPoolModel> CustomPools = [];

    public static void Register(RelicPoolModel pool)
    {
        if (ContentRegistry.IsFrozen)
        {
            MainFile.Logger.Warn(
                $"CustomRelicPoolRegistry: Register called after freeze for {pool.GetType().Name}");
            return;
        }

        if (!CustomPools.Contains(pool))
        {
            CustomPools.Add(pool);
            MainFile.Logger.Info($"CustomRelicPoolRegistry: registered {pool.GetType().Name}");
        }
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedRelicPools), MethodType.Getter)]
static class AllSharedRelicPoolsPatch
{
    [HarmonyPostfix]
    static IEnumerable<RelicPoolModel> AddCustomPools(IEnumerable<RelicPoolModel> __result)
    {
        return [.. __result, .. CustomRelicPoolRegistry.CustomPools];
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllRelics), MethodType.Getter)]
static class AllRelicsPatch
{
    [HarmonyPostfix]
    static IEnumerable<RelicModel> AddCustomPoolRelics(IEnumerable<RelicModel> __result)
    {
        // Custom relic pools such as WhatIf/Malice are not always part of the base
        // AllRelics enumeration on mobile, which leaves their compendium categories empty.
        return __result
            .Concat(CustomRelicPoolRegistry.CustomPools.SelectMany(pool => pool.AllRelics))
            .DistinctBy(relic => relic.Id);
    }
}
