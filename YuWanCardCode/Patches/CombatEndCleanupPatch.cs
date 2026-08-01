using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CombatManager))]
public class CombatEndCleanupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.EndCombatInternal), new Type[] { })]
    public static void EndCombatInternalPostfix()
    {
        RainDarkEffectPatch.CleanupAfterCombat();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.AfterCombatRoomLoaded))]
    public static void AfterCombatRoomLoadedPostfix()
    {
        RainDarkEffectPatch.TryApplyPendingRainEffect();
    }
}
