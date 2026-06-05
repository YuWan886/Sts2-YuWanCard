using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetReady))]
public static class StartRunLobbyReadyGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(StartRunLobby __instance, bool ready)
    {
        LobbyPlayer localPlayer = __instance.LocalPlayer;
        if (localPlayer.id == 0)
        {
            return true;
        }

        if (localPlayer.isReady == ready)
        {
            MainFile.Logger.Info(
                $"StartRunLobbyReadyGuard: ignored duplicate SetReady({ready}) for player {localPlayer.id}.");
            return false;
        }

        if (ready && AccessTools.Field(typeof(StartRunLobby), "_isBeginningRun")?.GetValue(__instance) is true)
        {
            MainFile.Logger.Info(
                $"StartRunLobbyReadyGuard: ignored SetReady(true) while run start is already in progress for player {localPlayer.id}.");
            return false;
        }

        return true;
    }
}
