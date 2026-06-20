using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NOrb), nameof(NOrb.UpdateVisuals))]
public static class CustomOrbTriggerIconPatch
{
    [HarmonyPostfix]
    static void Postfix(NOrb __instance)
    {
        if (__instance.Model is not YuWanOrbModel customOrb)
            return;

        var triggerTexture = customOrb.GetTriggerTexture();
        if (triggerTexture == null)
            return;

        var flashParticle = Traverse.Create(__instance).Field<CpuParticles2D>("_flashParticle").Value;
        if (flashParticle != null)
        {
            flashParticle.Texture = triggerTexture;
        }
    }
}
