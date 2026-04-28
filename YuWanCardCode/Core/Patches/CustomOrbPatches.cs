using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

public static class CustomOrbPatches
{
    [HarmonyPatch(typeof(OrbModel), "IconPath", MethodType.Getter)]
    static class IconPathPatch
    {
        static bool Prefix(OrbModel __instance, ref string __result)
        {
            if (__instance is YuWanOrbModel custom && custom.CustomIconPath is string path)
            {
                __result = path;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OrbModel), "SpritePath", MethodType.Getter)]
    static class SpritePathPatch
    {
        static bool Prefix(OrbModel __instance, ref string __result)
        {
            if (__instance is YuWanOrbModel custom && custom.CustomSpritePath is string path)
            {
                __result = path;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OrbModel), nameof(OrbModel.CreateSprite))]
    static class CreateSpritePatch
    {
        static bool Prefix(OrbModel __instance, ref Node2D __result)
        {
            if (__instance is not YuWanOrbModel custom)
                return true;

            var sprite = custom.CreateCustomSprite();
            if (sprite == null)
                return true;

            __result = sprite;
            return false;
        }
    }
}
