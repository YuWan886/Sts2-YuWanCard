using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunManager), "UpdateRichPresence")]
public static class RunManagerRichPresencePatch
{
    [HarmonyPrefix]
    public static bool Prefix(RunManager __instance)
    {
        var state = __instance.State;
        if (state == null || TestMode.IsOn)
        {
            return true;
        }

        var me = LocalContext.GetMe(state);
        if (me?.Character is Characters.Pig)
        {
            var netService = __instance.NetService;
            PlatformUtil.SetRichPresence("IN_RUN", netService.GetRawLobbyIdentifier(), state.Players.Count);
            PlatformUtil.SetRichPresenceValue("Character", "ironclad");
            PlatformUtil.SetRichPresenceValue("Act", state.Act.Id.Entry);
            PlatformUtil.SetRichPresenceValue("Ascension", state.AscensionLevel.ToString());
            return false;
        }

        return true;
    }
}