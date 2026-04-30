using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Adds a mod-specific prefix to model IDs for all IYuWanContent types.
/// The prefix is determined by the assembly name (e.g., "Watcher" -> "WATCHER-").
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry))]
static class IdPrefixPatch
{
    private static readonly ConcurrentDictionary<Type, string> IdCache = new();
    private static readonly ConcurrentDictionary<Assembly, string> AssemblyPrefixCache = new();

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
            var prefix = GetPrefixForAssembly(type.Assembly);
            __result = prefix + __result;
        }

        IdCache[type] = __result;
    }

    /// <summary>
    /// Gets the ID prefix for a given assembly.
    /// Uses the assembly name (e.g., "Watcher" -> "WATCHER-").
    /// Falls back to "YUWANCARD-" for the YuWanCard assembly.
    /// </summary>
    private static string GetPrefixForAssembly(Assembly assembly)
    {
        return AssemblyPrefixCache.GetOrAdd(assembly, asm =>
        {
            var assemblyName = asm.GetName().Name;
            
            // Special case for YuWanCard assembly
            if (assemblyName == "YuWanCard")
                return "YUWANCARD-";
            
            // For other assemblies, use the assembly name as prefix
            // e.g., "Watcher" -> "WATCHER-"
            return (assemblyName ?? "UNKNOWN").ToUpperInvariant() + "-";
        });
    }

    /// <summary>
    /// Manually sets the prefix for a specific assembly.
    /// This can be used by mods to customize their ID prefix.
    /// </summary>
    public static void SetPrefixForAssembly(Assembly assembly, string prefix)
    {
        AssemblyPrefixCache[assembly] = prefix;
    }
}
