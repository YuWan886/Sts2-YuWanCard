using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfTriplePlay : WhatIfRelicModel, IWhatIfUniformRelicSource
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromRelic<TriplePlay>();
    
    public WhatIfTriplePlay() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                rewards[i] = new RelicReward(ModelDb.Relic<TriplePlay>().ToMutable(), player);
            }
        }
        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return ModelDb.Relic<TriplePlay>();
    }
}
