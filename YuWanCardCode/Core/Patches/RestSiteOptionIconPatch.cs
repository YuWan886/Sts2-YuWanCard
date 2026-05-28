using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.RestSite;
using YuWanCard.RestSite;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(RestSiteOption), "Icon", MethodType.Getter)]
public static class RestSiteOptionIconPatch
{
    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is IYuWanRestSiteOption y && y.CustomIconPath != null)
        {
            __result = PreloadManager.Cache.GetTexture2D(y.CustomIconPath);
            return false;
        }
        return true;
    }
}
