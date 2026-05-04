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
            CustomPools.Add(pool);
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
