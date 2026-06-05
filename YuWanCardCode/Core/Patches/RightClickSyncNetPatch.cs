using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using YuWanCard.Core.RightClick;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NetClientGameService))]
public static class RightClickSyncClientPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetClientGameService.Update))]
    public static void OnClientUpdate(NetClientGameService __instance)
    {
        if (__instance.IsConnected)
        {
            YuWanRightClickMessageHandler.Register(__instance);
        }
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
public static class RightClickSyncHostPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetHostGameService.Update))]
    public static void OnHostUpdate(NetHostGameService __instance)
    {
        if (__instance.IsConnected)
        {
            YuWanRightClickMessageHandler.Register(__instance);
        }
    }
}
