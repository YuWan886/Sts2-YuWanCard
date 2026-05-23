using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class SkullGold : YuWanRelicModel
{
    private const int CombatBonusGold = 50;

    public override RelicRarity Rarity => RelicRarity.Event;

    public SkullGold() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        Flash();
        await PlayerCmd.GainGold(CombatBonusGold, Owner!);
    }
}
