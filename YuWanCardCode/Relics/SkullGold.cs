using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class SkullGold : YuWanRelicModel
{
    private const int DefaultGoldAmount = 30;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("GoldAmount", DefaultGoldAmount)];

    public SkullGold() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        Flash();
        await PlayerCmd.GainGold((int)DynamicVars["GoldAmount"].BaseValue, Owner!);
    }
}
