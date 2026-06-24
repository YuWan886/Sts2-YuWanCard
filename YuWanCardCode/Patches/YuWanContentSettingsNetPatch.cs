using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using YuWanCard.Multiplayer;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NetClientGameService))]
public static class YuWanContentSettingsClientNetPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetClientGameService.Update))]
    public static void OnClientUpdate(NetClientGameService __instance)
    {
        YuWanContentSettingsSync.UpdateClient(__instance);
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
public static class YuWanContentSettingsHostNetPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetHostGameService.Update))]
    public static void OnHostUpdate(NetHostGameService __instance)
    {
        YuWanContentSettingsSync.UpdateHost(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient))]
public static class YuWanContentSettingsCharacterSelectClientInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        YuWanContentSettingsSync.ForceClientRequest(__instance.Lobby.NetService);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsClient))]
public static class YuWanContentSettingsCustomRunClientInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        YuWanContentSettingsSync.ForceClientRequest(__instance.Lobby.NetService);
    }
}
