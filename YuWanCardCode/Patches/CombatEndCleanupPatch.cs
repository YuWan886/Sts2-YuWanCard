using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Cards;

namespace YuWanCard.Patches;

/// <summary>
/// 战斗结束时清理下雨特效的补丁
/// </summary>
[HarmonyPatch(typeof(CombatManager))]
public class CombatEndCleanupPatch
{
    /// <summary>
    /// 战斗结束时清理下雨特效
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.EndCombatInternal))]
    public static void EndCombatInternalPostfix()
    {
        RainDarkEffectPatch.CleanupAfterCombat();
        BugPig.ResetErrorCount();
    }

    /// <summary>
    /// 战斗场景加载完成后尝试应用挂起的下雨特效
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.AfterCombatRoomLoaded))]
    public static void AfterCombatRoomLoadedPostfix()
    {
        // 尝试应用挂起的下雨特效
        RainDarkEffectPatch.TryApplyPendingRainEffect();
    }
}
