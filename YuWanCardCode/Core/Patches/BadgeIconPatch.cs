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
        var customPath = CustomIconDir + __instance.Id.ToLowerInvariant() + ".png";
        if (!ResourceLoader.Exists(customPath)) return true;

        __result = PreloadManager.Cache.GetTexture2D(customPath);
        return false;
    }
}
