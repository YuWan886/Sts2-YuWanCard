using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ImageHelper))]
public static class ImageHelperPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ImageHelper.GetRoomIconPath))]
    public static bool GetRoomIconPathPrefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (modelId != null && modelId.Entry == "YUWANCARD-PIG_PIG")
        {
            __result = "res://YuWanCard/images/ancients/pig_pig_outline.png";
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ImageHelper.GetImagePath))]
    public static void GetImagePathPostfix(string innerPath, ref string __result)
    {
        if (innerPath == "events/blacksmith.png")
        {
            __result = "res://YuWanCard/images/events/blacksmith.png";
            return;
        }

        if (innerPath == "ui/rest_site/option_roast_pork.png")
        {
            __result = "res://YuWanCard/images/ui/rest_site/option_roast_pork.png";
            return;
        }

        if (innerPath == "ui/top_panel/character_icon_yuwancard-pig_outline.png")
        {
            __result = "res://YuWanCard/images/characters/character_icon_pig.png";
            return;
        }
    }
}
