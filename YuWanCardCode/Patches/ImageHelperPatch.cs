using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ImageHelper))]
public static class ImageHelperPatch
{
    [HarmonyPatch(nameof(ImageHelper.GetRoomIconPath))]
    [HarmonyPrefix]
    public static bool GetRoomIconPathPrefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (mapPointType == MapPointType.Ancient && modelId != null)
        {
            if (modelId.Entry == "YUWANCARD-PIG_PIG")
            {
                __result = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png";
                return false;
            }
        }
        return true;
    }

    [HarmonyPatch(nameof(ImageHelper.GetRoomIconOutlinePath))]
    [HarmonyPrefix]
    public static bool GetRoomIconOutlinePathPrefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (mapPointType == MapPointType.Ancient && modelId != null)
        {
            if (modelId.Entry == "YUWANCARD-PIG_PIG")
            {
                __result = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png";
                return false;
            }
        }
        return true;
    }
}
