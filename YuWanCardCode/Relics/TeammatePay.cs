using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class TeammatePay : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public TeammatePay() : base(true)
    {
    }

    public static bool HasTeammatePayRelic(Player? player)
    {
        if (player == null) return false;
        return player.Relics.Any(r => r is TeammatePay);
    }

    public static bool IsMultiplayerGame()
    {
        var netService = RunManager.Instance?.NetService;
        return netService != null && netService.IsConnected && 
               netService.Type != NetGameType.Singleplayer &&
               netService.Type != NetGameType.Replay;
    }
}
