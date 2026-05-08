using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Core.Badges;

namespace YuWanCard.Badges;

public class PigTycoonBadge : Badge
{
    public override BadgeRarity Rarity
    {
        get
        {
            int stacks = BadgeProgressTracker.GetProgress(_localPlayer.NetId, Id);
            if (stacks >= 200) return BadgeRarity.Gold;
            if (stacks >= 100) return BadgeRarity.Silver;
            if (stacks >= 50) return BadgeRarity.Bronze;
            return BadgeRarity.None;
        }
    }

    public PigTycoonBadge(SerializableRun run, ulong playerId, bool won)
        : base(run, won, playerId, "PIG_TYCOON", requiresWin: true, multiplayerOnly: false)
    {
    }

    public override bool IsObtained()
    {
        return Rarity != BadgeRarity.None;
    }
}
