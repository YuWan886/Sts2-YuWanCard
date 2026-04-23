using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Multiplayer;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NetClientGameService))]
public static class TeammatePayNetPatch
{
    private static bool _patchApplied = false;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetClientGameService.Update))]
    public static void OnUpdate(NetClientGameService __instance)
    {
        if (_patchApplied) return;
        if (!__instance.IsConnected) return;
        if (RunManager.Instance?.NetService == null) return;

        TeammatePayMessageHandler.Register();

        if (TeammatePayMessageHandler.IsRegistered)
        {
            _patchApplied = true;
        }
    }
}
