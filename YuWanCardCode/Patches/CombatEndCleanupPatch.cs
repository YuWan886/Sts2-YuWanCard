using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CombatManager))]
public class CombatEndCleanupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.EndCombatInternal), new Type[0])] // 0.111.1 起有两个重载，必须指定参数类型
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
