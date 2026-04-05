using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YuWanCard.Config;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(JoinFlow), "HandleInitialGameInfoMessage")]
public static class JoinFlowHandleInitialGameInfoMessagePatch
{
    private static readonly Logger Logger = new Logger("YuWanCard.HashPatch", LogType.Generic);

    [HarmonyPrefix]
    public static void Prefix(ref InitialGameInfoMessage message)
    {
        if (YuWanCardConfig.BypassModelDbHashCheck)
        {
            var originalHash = message.idDatabaseHash;
            message.idDatabaseHash = ModelIdSerializationCache.Hash;
            Logger.Info($"Bypassing ModelDb hash check: changed hash from {originalHash} to {message.idDatabaseHash}");
        }
    }
}
