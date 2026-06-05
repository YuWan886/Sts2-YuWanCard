using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Core.Badges;

namespace YuWanCard.Badges;

public class PigTycoonBadge : Badge
{
    public const string BadgeId = "PIG_TYCOON";

    public override string Id => BadgeId;

    public override bool RequiresWin => true;

    public override bool MultiplayerOnly => false;

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

    public PigTycoonBadge(SerializableRun run, ulong playerId)
        : base(run, playerId)
    {
    }

    public override bool IsObtained()
    {
        return Rarity != BadgeRarity.None;
    }
}
