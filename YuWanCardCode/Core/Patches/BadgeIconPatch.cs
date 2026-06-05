using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models.Badges;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(Badge), "get_BadgeIcon")]
public static class BadgeIconPatch
{
    private const string CustomIconDir = "res://YuWanCard/images/badges/";

    [HarmonyPrefix]
    public static bool BadgeIconPrefix(Badge __instance, ref Texture2D __result)
    {
        var customPath = ResolveCustomIconPath(__instance.Id);
        if (customPath == null) return true;

        __result = PreloadManager.Cache.GetTexture2D(customPath);
        return false;
    }

    private static string? ResolveCustomIconPath(string badgeId)
    {
        var autoPath = CustomIconDir + badgeId.ToLowerInvariant() + "_badge.png";
        if (ResourceLoader.Exists(autoPath))
        {
            return autoPath;
        }

        return null;
    }
}
