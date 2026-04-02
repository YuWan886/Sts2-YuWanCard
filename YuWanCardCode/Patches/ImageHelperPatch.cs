using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ImageHelper))]
public static class ImageHelperPatch
{
    private static readonly HashSet<string> ModEnchantmentIds = [];

    static ImageHelperPatch()
    {
        var assembly = typeof(ImageHelperPatch).Assembly;
        var enchantmentBaseType = typeof(EnchantmentModel);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && enchantmentBaseType.IsAssignableFrom(type))
            {
                var id = StringHelper.Slugify(type.Name).ToLowerInvariant();
                ModEnchantmentIds.Add(id);
            }
        }
    }

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

    [HarmonyPatch(nameof(ImageHelper.GetImagePath))]
    [HarmonyPostfix]
    public static void GetImagePathPostfix(string innerPath, ref string __result)
    {
        if (innerPath == "events/blacksmith.png")
        {
            __result = "res://YuWanCard/images/events/blacksmith.png";
            return;
        }

        if (innerPath.StartsWith("enchantments/"))
        {
            var fileName = innerPath["enchantments/".Length..];
            if (fileName.EndsWith(".png"))
            {
                var enchantmentId = fileName[..^4];
                if (ModEnchantmentIds.Contains(enchantmentId))
                {
                    var newPath = $"res://YuWanCard/images/{innerPath}";
                    if (ResourceLoader.Exists(newPath))
                    {
                        __result = newPath;
                    }
                }
            }
        }
    }
}
