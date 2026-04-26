using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using YuWanCard.Multiplayer;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NetClientGameService))]
public static class TeammatePayClientPatch
{
    private static bool _clientPatchApplied = false;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetClientGameService.Update))]
    public static void OnClientUpdate(NetClientGameService __instance)
    {
        if (_clientPatchApplied) return;
        if (!__instance.IsConnected) return;

        TeammatePayMessageHandler.Register(__instance);

        if (TeammatePayMessageHandler.IsRegistered)
        {
            _clientPatchApplied = true;
        }
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
public static class TeammatePayHostPatch
{
    private static bool _hostPatchApplied = false;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetHostGameService.Update))]
    public static void OnHostUpdate(NetHostGameService __instance)
    {
        if (_hostPatchApplied) return;
        if (!__instance.IsConnected) return;

        TeammatePayMessageHandler.Register(__instance);

        if (TeammatePayMessageHandler.IsRegistered)
        {
            _hostPatchApplied = true;
        }
    }
}
