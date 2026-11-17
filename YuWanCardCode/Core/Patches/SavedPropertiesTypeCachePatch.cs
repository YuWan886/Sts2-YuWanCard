using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.GetNetIdForPropertyName))]
public static class SavedPropertiesTypeCachePatch
{
    private static readonly Dictionary<string, int> PropertyNameToNetIdMap;
    private static readonly List<string> NetIdToPropertyNameMap;
    private static readonly PropertyInfo? PropertyIdBitSizeProperty;

    static SavedPropertiesTypeCachePatch()
    {
        PropertyNameToNetIdMap = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(ModelIdSerializationCache), "_propertyNameToNetIdMap");
        NetIdToPropertyNameMap = AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(ModelIdSerializationCache), "_netIdToPropertyNameMap");
        PropertyIdBitSizeProperty = AccessTools.Property(typeof(ModelIdSerializationCache), "PropertyIdBitSize");
    }

    public static void EnsureTypeRegistered(Type type)
    {
        ModelIdSerializationCache.CacheSavedPropertiesForTypeDebug(type);
        RefreshNetIdBitSize();
    }

    public static void EnsurePropertyNameRegistered(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        if (PropertyNameToNetIdMap.ContainsKey(propertyName))
        {
            return;
        }

        PropertyNameToNetIdMap[propertyName] = NetIdToPropertyNameMap.Count;
        NetIdToPropertyNameMap.Add(propertyName);
        RefreshNetIdBitSize();
    }

    private static void RefreshNetIdBitSize()
    {
        int newBitSize = (int)Math.Ceiling(Math.Log2(NetIdToPropertyNameMap.Count));
        PropertyIdBitSizeProperty?.SetValue(null, newBitSize);
    }
}
