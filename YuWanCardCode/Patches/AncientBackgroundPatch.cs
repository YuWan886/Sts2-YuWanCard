using System.Collections.Generic;
using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ImageHelper), "GetRoomIconPath")]
public static class ImageHelperGetRoomIconPathPatch
{
    private static readonly Dictionary<string, string> CustomAncientIconPaths = new()
    {
        ["YUWANCARD-PIG_PIG"] = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png"
    };
    
    [HarmonyPrefix]
    public static bool Prefix(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
    {
        if (mapPointType == MapPointType.Ancient && modelId != null)
        {
            if (CustomAncientIconPaths.TryGetValue(modelId.Entry, out var customPath))
            {
                __result = customPath;
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(ImageHelper), "GetImagePath")]
public static class ImageHelperGetImagePathPatch
{
    [HarmonyPrefix]
    public static bool Prefix(string innerPath, ref string __result)
    {
        if (innerPath == "ui/run_history/yuwancard-pig_pig_outline.png")
        {
            __result = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png";
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(AncientDialogueSet), "GetValidDialogues")]
public static class AncientDialogueSetGetValidDialoguesPatch
{
    [HarmonyPostfix]
    public static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits, int totalVisits, bool allowAnyCharacterDialogues, ref IEnumerable<AncientDialogue> __result)
    {
        var result = new List<AncientDialogue>(__result);
        if (result.Count == 0)
        {
            MainFile.Logger.Warn($"[AncientDialogueSetGetValidDialoguesPatch] No valid dialogues found! Returning fallback dialogue.");
            
            // 返回一个默认的 ANY 对话
            var fallbackDialogue = new AncientDialogue("event:/sfx/ui/enchant_simple")
            {
                VisitIndex = 0,
                IsRepeating = true
            };
            
            // 使用反射设置 Lines 的 LineText 和 Speaker
            var lines = fallbackDialogue.Lines;
            if (lines.Count > 0)
            {
                var line = lines[0];
                var lineTextProperty = line.GetType().GetProperty("LineText");
                var speakerProperty = line.GetType().GetProperty("Speaker");
                if (lineTextProperty != null && speakerProperty != null)
                {
                    var locString = new LocString("ancients", $"YUWANCARD-PIG_PIG.talk.ANY.0-0.ancient");
                    lineTextProperty.SetValue(line, locString);
                    speakerProperty.SetValue(line, AncientDialogueSpeaker.Ancient);
                }
            }
            
            result.Add(fallbackDialogue);
        }
        __result = result;
    }
}

[HarmonyPatch(typeof(EventModel), "BackgroundScenePath", MethodType.Getter)]
public static class BackgroundScenePathPatch
{
    [HarmonyPrefix]
    public static bool Prefix(EventModel __instance, ref string __result)
    {
        if (__instance is CustomAncientModel customAncient && customAncient.CustomScenePath != null)
        {
            __result = customAncient.CustomScenePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CustomAncientModel), "GetAssetPaths")]
public static class CustomAncientGetAssetPathsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CustomAncientModel __instance, IRunState runState, ref IEnumerable<string> __result)
    {
        var basePaths = new List<string>();
        
        basePaths.Add("res://scenes/events/ancient_event_layout.tscn");
        
        if (__instance.CustomScenePath != null)
            basePaths.Add(__instance.CustomScenePath);
        
        __result = basePaths;
        return false;
    }
}

[HarmonyPatch(typeof(AncientEventModel), "MapNodeAssetPaths", MethodType.Getter)]
public static class MapNodeAssetPathsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(AncientEventModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance is CustomAncientModel custom)
        {
            var paths = new List<string>();
            if (custom.CustomMapIconPath != null)
                paths.Add(custom.CustomMapIconPath);
            else
            {
                var defaultPath = ImageHelper.GetImagePath("packed/map/ancients/ancient_node_" + __instance.Id.Entry.ToLowerInvariant() + ".png");
                paths.Add(defaultPath);
            }

            if (custom.CustomMapIconOutlinePath != null)
                paths.Add(custom.CustomMapIconOutlinePath);
            else
            {
                var defaultOutlinePath = ImageHelper.GetImagePath("packed/map/ancients/ancient_node_" + __instance.Id.Entry.ToLowerInvariant() + ".png");
                paths.Add(defaultOutlinePath);
            }

            __result = paths;
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
