using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
public static class SavedPropertiesTypeCachePatch
{
    private const string RitsuLibHarmonyId = "com.ritsukage.sts2-RitsuLib";

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
                if (SavedPropertiesTypeCache.GetJsonPropertiesForType(type) == null)
                {
                    throw new InvalidOperationException(
                        $"Saved property type {type.FullName} was registered after cache finalization.");
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
            if (_initialized)
            {
                if (!PropertyNameToNetIdMap.ContainsKey(propertyName))
                {
                    throw new InvalidOperationException(
                        $"Saved property name {propertyName} was registered after cache finalization.");
                }

                return;
            }

            PendingPropertyNames.Add(propertyName);
        }
    }

    [HarmonyPrefix]
    [HarmonyBefore(RitsuLibHarmonyId)]
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

        foreach (Type type in pendingTypes)
        {
            if (SavedPropertiesTypeCache.GetJsonPropertiesForType(type) == null)
            {
                SavedPropertiesTypeCache.InjectTypeIntoCache(type);
            }
        }

        foreach (string propertyName in pendingPropertyNames)
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
