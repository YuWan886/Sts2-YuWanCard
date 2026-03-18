using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))]
public class AncientBackgroundPatch
{
    [HarmonyPrefix]
    public static bool Prefix(EventModel __instance, ref PackedScene __result)
    {
        if (__instance.Id.Entry != "YUWANCARD-PIG_PIG")
            return true;

        __result = PreloadManager.Cache.GetScene("res://YuWanCard/scenes/ancients/pig_pig.tscn");
        return false;
    }
}
