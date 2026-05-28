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
            __result = "res://YuWanCard/images/ancients/pig_pig.png";
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ImageHelper.GetRoomIconOutlinePath))]
    public static bool GetRoomIconOutlinePathPrefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (modelId != null && modelId.Entry == "YUWANCARD-PIG_PIG")
        {
            __result = "res://YuWanCard/images/ancients/pig_pig.png";
            return false;
        }
        return true;
    }
}



