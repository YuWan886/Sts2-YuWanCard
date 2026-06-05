using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ImageHelper))]
public static class ImageHelperPatch
{
    private const string PigPigModelId = "YUWANCARD-PIG_PIG";
    private const string PigPigIconPath = "res://YuWanCard/images/ancients/pig_pig.png";
    private const string IgnisBossEncounterId = "YUWANCARD-IGNIS_BOSS";
    private const string IgnisBossRunHistoryIconPath = "res://YuWanCard/images/ui/run_history/ignis_bos.png";

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ImageHelper.GetRoomIconPath))]
    public static bool GetRoomIconPathPrefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (modelId != null && modelId.Entry == PigPigModelId)
        {
            __result = PigPigIconPath;
            return false;
        }

        if (roomType == RoomType.Boss && modelId != null && modelId.Entry == IgnisBossEncounterId)
        {
            __result = IgnisBossRunHistoryIconPath;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ImageHelper.GetRoomIconOutlinePath))]
    public static bool GetRoomIconOutlinePathPrefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (modelId != null && modelId.Entry == PigPigModelId)
        {
            __result = PigPigIconPath;
            return false;
        }

        if (roomType == RoomType.Boss && modelId != null && modelId.Entry == IgnisBossEncounterId)
        {
            __result = IgnisBossRunHistoryIconPath;
            return false;
        }
        return true;
    }
}


