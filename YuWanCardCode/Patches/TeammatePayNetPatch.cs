using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using YuWanCard.Multiplayer;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NetClientGameService))]
public static class TeammatePayClientPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetClientGameService.Update))]
    public static void OnClientUpdate(NetClientGameService __instance)
    {
        if (!__instance.IsConnected) return;

        TeammatePayMessageHandler.Register(__instance);
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
public static class TeammatePayHostPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetHostGameService.Update))]
    public static void OnHostUpdate(NetHostGameService __instance)
    {
        if (!__instance.IsConnected) return;

        TeammatePayMessageHandler.Register(__instance);
    }
}
