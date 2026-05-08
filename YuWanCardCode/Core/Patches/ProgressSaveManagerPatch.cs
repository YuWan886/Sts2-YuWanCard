using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;

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

    /// <summary>
    /// PostRunUnlockCharacterEpochCheck calls ModelDb.GetById on the
    /// saved character ID (e.g. CHARACTER.YUWANCARD-PIG). Custom
    /// characters may not be resolveable via the internal dictionary,
    /// causing a ModelNotFoundException. Skip for mod characters.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("PostRunUnlockCharacterEpochCheck")]
    private static bool Prefix_PostRunUnlockCharacterEpochCheck(
        SerializablePlayer serializablePlayer)
    {
        var id = serializablePlayer.CharacterId;
        if (id == null)
            return false;
        var character = ModelDb.GetByIdOrNull<CharacterModel>(id);
        if (character == null || character is IYuWanCharacter)
            return false;
        return true;
    }
}
