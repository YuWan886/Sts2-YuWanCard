using System.Buffers.Binary;
using System.IO.Hashing;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Timeline;
using YuWanCard.Timeline;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
static class ModelIdSerializationCachePatch
{
    private static readonly Dictionary<string, int> CategoryNameToNetIdMap =
        AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(ModelIdSerializationCache), "_categoryNameToNetIdMap");

    private static readonly List<string> NetIdToCategoryNameMap =
        AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(ModelIdSerializationCache), "_netIdToCategoryNameMap");

    private static readonly Dictionary<string, int> EntryNameToNetIdMap =
        AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(ModelIdSerializationCache), "_entryNameToNetIdMap");

    private static readonly List<string> NetIdToEntryNameMap =
        AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(ModelIdSerializationCache), "_netIdToEntryNameMap");

    private static readonly Dictionary<string, int> EpochNameToNetIdMap =
        AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(ModelIdSerializationCache), "_epochNameToNetIdMap");

    private static readonly List<string> NetIdToEpochNameMap =
        AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(ModelIdSerializationCache), "_netIdToEpochNameMap");

    private static readonly PropertyInfo? CategoryIdBitSizeProperty =
        AccessTools.Property(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.CategoryIdBitSize));

    private static readonly PropertyInfo? EntryIdBitSizeProperty =
        AccessTools.Property(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EntryIdBitSize));

    private static readonly PropertyInfo? EpochIdBitSizeProperty =
        AccessTools.Property(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EpochIdBitSize));

    private static readonly PropertyInfo? HashProperty =
        AccessTools.Property(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Hash));

    [HarmonyPrefix]
    static bool InitPrefix()
    {
        PigTimelineRegistry.EnsureRegistered();

        byte[] scratch = new byte[512];
        var hash = new XxHash32();

        ResetCaches();

        var uniqueEntries = CollectUniqueModelTypes();
        uniqueEntries.Sort(CompareEntries);

        var canonicalById = new Dictionary<ModelId, CacheTypeEntry>();
        foreach (CacheTypeEntry entry in uniqueEntries)
        {
            ModelId id = ModelDb.GetId(entry.Type);
            if (canonicalById.TryGetValue(id, out CacheTypeEntry existing)
                && existing.IdentityKey != entry.IdentityKey)
            {
                Log.Warn(
                    $"Two AbstractModels {existing.Type} and {entry.Type} from mod {entry.ModId ?? existing.ModId} share an ID! This might break multiplayer.");
            }
            else
            {
                canonicalById[id] = entry;
            }

            RegisterIfMissing(CategoryNameToNetIdMap, NetIdToCategoryNameMap, id.Category);
            RegisterIfMissing(EntryNameToNetIdMap, NetIdToEntryNameMap, id.Entry);

            int bytes = Encoding.UTF8.GetBytes(id.Category, 0, id.Category.Length, scratch, 0);
            hash.Append(scratch.AsSpan(0, bytes));
            bytes = Encoding.UTF8.GetBytes(id.Entry, 0, id.Entry.Length, scratch, 0);
            hash.Append(scratch.AsSpan(0, bytes));
        }

        foreach (string epochId in EpochModel.AllEpochIds)
        {
            RegisterIfMissing(EpochNameToNetIdMap, NetIdToEpochNameMap, epochId);

            int bytes = Encoding.UTF8.GetBytes(epochId, 0, epochId.Length, scratch, 0);
            hash.Append(scratch.AsSpan(0, bytes));
        }

        SetBitSize(CategoryIdBitSizeProperty, NetIdToCategoryNameMap.Count);
        SetBitSize(EntryIdBitSizeProperty, NetIdToEntryNameMap.Count);
        SetBitSize(EpochIdBitSizeProperty, NetIdToEpochNameMap.Count);

        BinaryPrimitives.WriteInt32LittleEndian(scratch.AsSpan(), NetIdToCategoryNameMap.Count);
        hash.Append(scratch.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32LittleEndian(scratch.AsSpan(), NetIdToEntryNameMap.Count);
        hash.Append(scratch.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32LittleEndian(scratch.AsSpan(), NetIdToEpochNameMap.Count);
        hash.Append(scratch.AsSpan(0, 4));

        uint currentHash = hash.GetCurrentHashAsUInt32();
        HashProperty?.SetValue(null, currentHash);

        int dedupedCount = CountOriginalEntries() - uniqueEntries.Count;
        if (dedupedCount > 0)
        {
            MainFile.Logger.Info($"ModelIdSerializationCache: deduplicated {dedupedCount} duplicate type entries before hashing.");
        }

        Log.Info(
            $"ModelIdSerializationCache initialized. Categories: {NetIdToCategoryNameMap.Count} Entries: {NetIdToEntryNameMap.Count} Epochs: {NetIdToEpochNameMap.Count} Hash: {currentHash}");
        return false;
    }

    private static void ResetCaches()
    {
        CategoryNameToNetIdMap.Clear();
        CategoryNameToNetIdMap[ModelId.none.Category] = 0;
        NetIdToCategoryNameMap.Clear();
        NetIdToCategoryNameMap.Add(ModelId.none.Category);

        EntryNameToNetIdMap.Clear();
        EntryNameToNetIdMap[ModelId.none.Entry] = 0;
        NetIdToEntryNameMap.Clear();
        NetIdToEntryNameMap.Add(ModelId.none.Entry);

        EpochNameToNetIdMap.Clear();
        NetIdToEpochNameMap.Clear();
    }

    private static List<CacheTypeEntry> CollectUniqueModelTypes()
    {
        var entriesByIdentity = new Dictionary<string, CacheTypeEntry>(StringComparer.Ordinal);

        foreach (Type type in AbstractModelSubtypes.All)
        {
            AddOrUpdate(entriesByIdentity, type, modId: null);
        }

        foreach (Mod mod in ModManager.Mods)
        {
            if (mod.state != ModLoadState.Loaded || mod.assembly == null)
            {
                continue;
            }

            string? modId = mod.manifest?.id;
            foreach (Type type in ReflectionHelper.GetSubtypesFromAssembly(mod.assembly, typeof(AbstractModel)))
            {
                AddOrUpdate(entriesByIdentity, type, modId);
            }
        }

        return entriesByIdentity.Values.ToList();
    }

    private static void AddOrUpdate(
        Dictionary<string, CacheTypeEntry> entriesByIdentity,
        Type type,
        string? modId)
    {
        string identityKey = GetIdentityKey(type);
        if (entriesByIdentity.TryGetValue(identityKey, out CacheTypeEntry existing))
        {
            if (existing.ModId == null && modId != null)
            {
                entriesByIdentity[identityKey] = existing with { ModId = modId };
            }
            return;
        }

        entriesByIdentity[identityKey] = new CacheTypeEntry(type, modId, identityKey);
    }

    private static int CompareEntries(CacheTypeEntry left, CacheTypeEntry right)
    {
        int result = string.CompareOrdinal(left.Type.Name, right.Type.Name);
        if (result != 0)
        {
            return result;
        }

        if (left.ModId != null && right.ModId == null)
        {
            return 1;
        }

        if (left.ModId == null && right.ModId != null)
        {
            return -1;
        }

        result = string.CompareOrdinal(left.ModId, right.ModId);
        if (result != 0)
        {
            return result;
        }

        return string.CompareOrdinal(left.IdentityKey, right.IdentityKey);
    }

    private static string GetIdentityKey(Type type)
    {
        string assemblyName = type.Assembly.GetName().Name ?? "<unknown>";
        string fullName = type.FullName ?? type.Name;
        return $"{assemblyName}:{fullName}";
    }

    private static int CountOriginalEntries()
    {
        int count = AbstractModelSubtypes.All.Count;
        foreach (Mod mod in ModManager.Mods)
        {
            if (mod.state != ModLoadState.Loaded || mod.assembly == null)
            {
                continue;
            }

            count += ReflectionHelper.GetSubtypesFromAssembly(mod.assembly, typeof(AbstractModel)).Count();
        }

        return count;
    }

    private static void RegisterIfMissing(
        Dictionary<string, int> nameToNetIdMap,
        List<string> netIdToNameMap,
        string value)
    {
        if (nameToNetIdMap.ContainsKey(value))
        {
            return;
        }

        nameToNetIdMap[value] = netIdToNameMap.Count;
        netIdToNameMap.Add(value);
    }

    private static void SetBitSize(PropertyInfo? property, int count)
    {
        int bitSize = count <= 1 ? 0 : (int)Math.Ceiling(Math.Log2(count));
        property?.SetValue(null, bitSize);
    }

    private readonly record struct CacheTypeEntry(Type Type, string? ModId, string IdentityKey);
}
