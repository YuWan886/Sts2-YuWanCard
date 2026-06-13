using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Powers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
public static class YouArePigAnimationPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, string trigger)
    {
        __instance.Entity?.GetPower<YouArePigPower>()?.TriggerPigAnimation(trigger);
    }
}
