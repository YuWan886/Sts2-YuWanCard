using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Core.Badges;

namespace YuWanCard.Badges;

public sealed class WerewolfBadge : Badge
{
    public const string BadgeId = "WEREWOLF";

    public override BadgeRarity Rarity =>
        BadgeProgressTracker.GetProgress(_localPlayer.NetId, Id) > 0
            ? BadgeRarity.Silver
            : BadgeRarity.None;

    public WerewolfBadge(SerializableRun run, ulong playerId, bool won)
        : base(run, won, playerId, BadgeId, requiresWin: false, multiplayerOnly: true)
    {
    }

    public override bool IsObtained()
    {
        return Rarity != BadgeRarity.None;
    }
}
