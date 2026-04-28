using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(CardPoolModel), "FrameMaterial", MethodType.Getter)]
static class CardPoolMaterialPatch
{
    private static readonly Dictionary<Type, ShaderMaterial> _poolMaterials = [];

    [HarmonyPrefix]
    static bool UseCustomMaterial(CardPoolModel __instance, ref Material __result)
    {
        if (__instance is YuWanCardPoolModel yuWanPool)
        {
            if (!yuWanPool.CardFrameMaterialPath.Equals("card_frame_red"))
                return true;

            if (!_poolMaterials.TryGetValue(__instance.GetType(), out ShaderMaterial? shaderMaterial))
            {
                shaderMaterial = ShaderUtils.GenerateHsv(yuWanPool.H, yuWanPool.S, yuWanPool.V);
                _poolMaterials[__instance.GetType()] = shaderMaterial;
            }

            __result = shaderMaterial;
            return false;
        }
        return true;
    }
}
