using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using YuWanCard.Cards;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CombatManager))]
public class CombatEndCleanupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.EndCombatInternal))]
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
