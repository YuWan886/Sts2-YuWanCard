using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Adds the YuWanCard-specific prefix to IDs for this assembly's custom models.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry))]
static class IdPrefixPatch
{
    private static readonly ConcurrentDictionary<Type, string> IdCache = new();
    private static readonly Assembly ThisAssembly = typeof(IYuWanContent).Assembly;
    private const string Prefix = "YUWANCARD-";

    [HarmonyPostfix]
    static void AddPrefix(ref string __result, Type type)
    {
        // ModelDb.GetEntry is global. Do not rewrite IDs owned by dependent or unrelated mods.
        if (type.Assembly != ThisAssembly || !typeof(IYuWanContent).IsAssignableFrom(type))
            return;

        if (IdCache.TryGetValue(type, out var cached))
        {
            __result = cached;
            return;
        }

        __result = IdCache.GetOrAdd(type, Prefix + __result);
    }
}
