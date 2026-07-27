using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YuWanCard.Timeline;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
static class PigTimelineSerializationCachePatch
{
    [HarmonyPrefix]
    static void RegisterTimelineContent()
    {
        PigTimelineRegistry.EnsureRegistered();
    }
}

[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
static class ModelIdSerializationCachePatch
{
    private static readonly MethodInfo EntryListSortMethod = AccessTools.Method(
        typeof(List<(Type, Mod)>),
        nameof(List<(Type, Mod)>.Sort),
        [typeof(Comparison<(Type, Mod)>)])!;

    private static readonly MethodInfo DeduplicateAndSortMethod = AccessTools.Method(
        typeof(ModelIdSerializationCachePatch),
        nameof(DeduplicateAndSort))!;

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> DeduplicateModelTypes(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Callvirt
                && Equals(instruction.operand, EntryListSortMethod))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = DeduplicateAndSortMethod;
            }

            yield return instruction;
        }

        // 0.109+ builds the cache from ModelDb.All through ContentSorter, so the
        // legacy List.Sort call is absent and no type-list deduplication is needed.
    }

    private static void DeduplicateAndSort(
        List<(Type Type, Mod? Mod)> entries,
        Comparison<(Type Type, Mod? Mod)> comparison)
    {
        var indexByIdentity = new Dictionary<string, int>(StringComparer.Ordinal);
        var deduplicated = new List<(Type Type, Mod? Mod)>(entries.Count);

        foreach ((Type type, Mod? mod) in entries)
        {
            string identity = GetTypeIdentityKey(type);
            if (!indexByIdentity.TryGetValue(identity, out int existingIndex))
            {
                indexByIdentity[identity] = deduplicated.Count;
                deduplicated.Add((type, mod));
                continue;
            }

            if (deduplicated[existingIndex].Mod == null && mod != null)
            {
                deduplicated[existingIndex] = (type, mod);
            }
        }

        int removedCount = entries.Count - deduplicated.Count;
        entries.Clear();
        entries.AddRange(deduplicated);
        entries.Sort(comparison);

        if (removedCount > 0)
        {
            MainFile.Logger.Info(
                $"ModelIdSerializationCache: deduplicated {removedCount} duplicate type entries before hashing.");
        }
    }

    private static string GetTypeIdentityKey(Type type)
    {
        string assemblyName = type.Assembly.GetName().Name ?? "<unknown>";
        string fullName = type.FullName ?? type.Name;
        return $"{assemblyName}:{fullName}";
    }
}
