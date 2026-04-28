using System.Collections.Concurrent;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Adds the YUWANCARD- prefix to model IDs for all IYuWanContent types.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry))]
static class IdPrefixPatch
{
    private const string ModPrefix = "YUWANCARD-";
    private static readonly ConcurrentDictionary<Type, string> IdCache = new();

    [HarmonyPostfix]
    static void AddPrefix(ref string __result, Type type)
    {
        if (IdCache.TryGetValue(type, out var cached))
        {
            __result = cached;
            return;
        }

        if (typeof(IYuWanContent).IsAssignableFrom(type))
        {
            __result = ModPrefix + __result;
        }

        IdCache[type] = __result;
    }
}
