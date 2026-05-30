using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfSeriesRelics : WhatIfRelicModel
{
    private static readonly Lazy<RelicModel[]> SevenSinRelics = new(() =>
    [
        ModelDb.Relic<ArrogantPig>(),
        ModelDb.Relic<JealousPig>(),
        ModelDb.Relic<FuriousPig>(),
        ModelDb.Relic<LazyPig>(),
        ModelDb.Relic<GreedyPig>(),
        ModelDb.Relic<GluttonousPig>(),
        ModelDb.Relic<LustfulPig>()
    ]);

    public WhatIfSeriesRelics() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        var relics = SevenSinRelics.Value;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                var sinRelic = relics[Owner!.RunState.Rng.Niche.NextInt(relics.Length)].ToMutable();
                rewards[i] = new RelicReward(sinRelic, player);
            }
        }
        return true;
    }
}
