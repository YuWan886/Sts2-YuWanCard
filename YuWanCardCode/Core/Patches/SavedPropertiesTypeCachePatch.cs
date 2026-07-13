using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.Init))]
public static class SavedPropertiesTypeCachePatch
{
    private static readonly object LockObj = new();
    private static readonly HashSet<Type> PendingTypes = [];
    private static readonly SortedSet<string> PendingPropertyNames = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, int> PropertyNameToNetIdMap =
        AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(SavedPropertiesTypeCache), "_propertyNameToNetIdMap");

    private static readonly List<string> NetIdToPropertyNameMap =
        AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap");

    private static readonly PropertyInfo? NetIdBitSizeProperty =
        AccessTools.Property(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.NetIdBitSize));

    private static bool _initialized;

    public static void EnsureTypeRegistered(Type type)
    {
        lock (LockObj)
        {
            if (_initialized)
            {
                if (ModelDb.All.All(model => model.GetType() != type))
                {
                    throw new InvalidOperationException(
                        $"Saved property type {type.FullName} was registered after SavedPropertiesTypeCache.Init.");
                }

                return;
            }

            PendingTypes.Add(type);
        }
    }

    public static void EnsurePropertyNameRegistered(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        lock (LockObj)
        {
            if (PropertyNameToNetIdMap.ContainsKey(propertyName))
            {
                return;
            }

            if (_initialized)
            {
                throw new InvalidOperationException(
                    $"Saved property name {propertyName} was registered after SavedPropertiesTypeCache.Init.");
            }

            PendingPropertyNames.Add(propertyName);
        }
    }

    [HarmonyPostfix]
    static void RegisterPendingEntries()
    {
        Type[] pendingTypes;
        string[] pendingPropertyNames;
        lock (LockObj)
        {
            pendingTypes = PendingTypes
                .OrderBy(type => type.Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(type => type.FullName ?? type.Name, StringComparer.Ordinal)
                .ToArray();
            pendingPropertyNames = PendingPropertyNames.ToArray();
        }

        var canonicalTypes = ModelDb.All
            .Select(model => model.GetType())
            .ToHashSet();

        foreach (var type in pendingTypes)
        {
            if (!canonicalTypes.Contains(type))
            {
                SavedPropertiesTypeCache.InjectTypeIntoCache(type);
            }
        }

        foreach (var propertyName in pendingPropertyNames)
        {
            if (PropertyNameToNetIdMap.ContainsKey(propertyName))
            {
                continue;
            }

            PropertyNameToNetIdMap[propertyName] = NetIdToPropertyNameMap.Count;
            NetIdToPropertyNameMap.Add(propertyName);
        }

        int count = NetIdToPropertyNameMap.Count;
        int bitSize = count <= 1 ? 0 : (int)Math.Ceiling(Math.Log2(count));
        NetIdBitSizeProperty?.SetValue(null, bitSize);

        lock (LockObj)
        {
            _initialized = true;
        }
    }
}
