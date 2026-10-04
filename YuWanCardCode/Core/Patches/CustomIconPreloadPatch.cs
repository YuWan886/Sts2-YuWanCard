using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(PreloadManager), "GetRunAssetPaths")]
static class CustomIconPreloadPatch
{
    [HarmonyPostfix]
    static void AddCustomIcons(ref IEnumerable<string> __result)
    {
        __result = __result.Concat(GetCustomIconPaths()).Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetCustomIconPaths()
    {
        foreach (YuWanPowerModel power in ModelDb.AllPowers.OfType<YuWanPowerModel>())
        {
            if (power.CustomPackedIconPath is { } packedIconPath)
            {
                yield return packedIconPath;
            }

            if (power.CustomBigIconPath is { } bigIconPath)
            {
                yield return bigIconPath;
            }
        }

        foreach (YuWanCardPoolModel pool in ModelDb.AllCardPools.OfType<YuWanCardPoolModel>())
        {
            if (pool.BigEnergyIconPath is { } bigIconPath)
            {
                yield return bigIconPath;
            }

            if (pool.TextEnergyIconPath is { } textIconPath)
            {
                yield return textIconPath;
            }
        }
    }
}
