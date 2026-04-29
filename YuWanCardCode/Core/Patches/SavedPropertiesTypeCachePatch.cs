using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Patches;

[HarmonyPatch]
public static class SavedPropertiesTypeCachePatch
{
    private static readonly Dictionary<string, int> PropertyNameToNetIdMap;
    private static readonly List<string> NetIdToPropertyNameMap;

    static SavedPropertiesTypeCachePatch()
    {
        PropertyNameToNetIdMap = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(SavedPropertiesTypeCache), "_propertyNameToNetIdMap");
        NetIdToPropertyNameMap = AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.GetNetIdForPropertyName))]
    public static bool GetNetIdForPropertyName(string propertyName, ref int __result)
    {
        if (!PropertyNameToNetIdMap.TryGetValue(propertyName, out int netId))
        {
            netId = NetIdToPropertyNameMap.Count;
            PropertyNameToNetIdMap[propertyName] = netId;
            NetIdToPropertyNameMap.Add(propertyName);

            int newBitSize = (int)Math.Ceiling(Math.Log2(NetIdToPropertyNameMap.Count));
            AccessTools.Property(typeof(SavedPropertiesTypeCache), "NetIdBitSize")
                ?.SetValue(null, newBitSize);
        }

        __result = netId;
        return false;
    }
}
