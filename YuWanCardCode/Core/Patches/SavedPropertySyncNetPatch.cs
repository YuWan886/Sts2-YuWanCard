using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NetClientGameService))]
public static class SavedPropertySyncClientPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetClientGameService.Update))]
    public static void OnClientUpdate(NetClientGameService __instance)
    {
        if (__instance.IsConnected)
        {
            SavedPropertySyncMessageHandler.Register(__instance);
        }
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
public static class SavedPropertySyncHostPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetHostGameService.Update))]
    public static void OnHostUpdate(NetHostGameService __instance)
    {
        if (__instance.IsConnected)
        {
            SavedPropertySyncMessageHandler.Register(__instance);
        }
    }
}
