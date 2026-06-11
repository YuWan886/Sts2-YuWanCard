using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Patches.UI;

namespace YuWanCard.Core.Patches;

internal static class YummyCookieIconPatchHelper
{
    public static RelicIconData? GetCustomIconData(RelicModel relic)
    {
        if (relic is not YummyCookie || relic.IsCanonical)
        {
            return null;
        }

        CharacterModel? character = relic.Owner?.Character;
        if (character == null)
        {
            character = LocalContext.GetMe(RunManager.Instance?.State)?.Character;
        }

        return (character as IYuWanCharacter)?.CustomYummyCookie;
    }
}

[HarmonyPriority(Priority.First)]
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.PackedIconPath), MethodType.Getter)]
static class YummyCookiePackedIconPathPatch
{
    static bool Prefix(RelicModel __instance, ref string? __result)
    {
        RelicIconData? customIcon = YummyCookieIconPatchHelper.GetCustomIconData(__instance);
        if (customIcon == null)
        {
            return true;
        }

        __result = customIcon.PackedIconPath;
        return false;
    }
}

[HarmonyPriority(Priority.First)]
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.IconOutline), MethodType.Getter)]
static class YummyCookieIconOutlinePatch
{
    static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        RelicIconData? customIcon = YummyCookieIconPatchHelper.GetCustomIconData(__instance);
        if (customIcon == null)
        {
            return true;
        }

        __result = ResourceLoader.Load<Texture2D>(customIcon.PackedIconOutlinePath, null, ResourceLoader.CacheMode.Reuse);
        return false;
    }
}

[HarmonyPriority(Priority.First)]
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.BigIcon), MethodType.Getter)]
static class YummyCookieBigIconPatch
{
    static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        RelicIconData? customIcon = YummyCookieIconPatchHelper.GetCustomIconData(__instance);
        if (customIcon == null)
        {
            return true;
        }

        __result = PreloadManager.Cache.GetTexture2D(customIcon.BigIconPath);
        return false;
    }
}
