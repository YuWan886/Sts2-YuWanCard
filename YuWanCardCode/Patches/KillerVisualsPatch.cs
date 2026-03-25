using System.Reflection;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Monsters;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public class KillerVisualsPatch
{
    [HarmonyPrefix]
    [Obsolete]
    public static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        if (__instance is Killer)
        {
            __result = GodotUtils.CreatureVisualsFromScene("res://YuWanCard/scenes/monsters/killer/killer_visuals.tscn");
            return false;
        }
        return true;
    }
}
