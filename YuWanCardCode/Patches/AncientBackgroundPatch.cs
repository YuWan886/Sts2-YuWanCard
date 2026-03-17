using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ImageHelper), "GetImagePath")]
public static class ImageHelperGetImagePathPatch
{
    private static readonly Dictionary<string, string> CustomImagePaths = new()
    {
        ["ui/run_history/yuwancard-pig_pig.png"] = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png",
        ["ui/run_history/yuwancard-pig_pig_outline.png"] = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png"
    };
    
    [HarmonyPrefix]
    public static bool Prefix(string innerPath, ref string __result)
    {
        if (CustomImagePaths.TryGetValue(innerPath, out var customPath))
        {
            __result = customPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(SceneHelper), "GetScenePath")]
public static class SceneHelperGetScenePathPatch
{
    private static readonly Dictionary<string, string> CustomScenePaths = new()
    {
        ["events/background_scenes/yuwancard-pig_pig"] = "res://YuWanCard/scenes/ancients/pig_pig.tscn"
    };
    
    [HarmonyPrefix]
    public static bool Prefix(string innerPath, ref string __result)
    {
        if (CustomScenePaths.TryGetValue(innerPath, out var customPath))
        {
            __result = customPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(ActModel), "AssetPaths", MethodType.Getter)]
public static class ActModelAssetPathsPatch
{
    private const string PigPigRunHistoryIcon = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png";
    private const string PigPigRunHistoryIconOutline = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png";
    
    [HarmonyPostfix]
    public static void Postfix(ActModel __instance, ref IEnumerable<string> __result)
    {
        var paths = new List<string>(__result);
        
        if (__instance._rooms.HasAncient && __instance._rooms.Ancient.Id.Entry == "YUWANCARD-PIG_PIG")
        {
            if (!paths.Contains(PigPigRunHistoryIcon))
                paths.Add(PigPigRunHistoryIcon);
            
            if (!paths.Contains(PigPigRunHistoryIconOutline))
                paths.Add(PigPigRunHistoryIconOutline);
        }
        
        __result = paths;
    }
}
