using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ProgressSaveManager))]
public static class ProgressSaveManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("CheckFifteenElitesDefeatedEpoch")]
    private static bool Prefix_CheckFifteenElitesDefeatedEpoch(Player localPlayer)
    {
        if (localPlayer.Character is IYuWanCharacter)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("CheckFifteenBossesDefeatedEpoch")]
    private static bool Prefix_CheckFifteenBossesDefeatedEpoch(Player localPlayer)
    {
        if (localPlayer.Character is IYuWanCharacter)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ObtainCharUnlockEpoch")]
    private static bool Prefix_ObtainCharUnlockEpoch(Player localPlayer, int act)
    {
        if (localPlayer.Character is IYuWanCharacter)
        {
            return false;
        }
        return true;
    }
}
