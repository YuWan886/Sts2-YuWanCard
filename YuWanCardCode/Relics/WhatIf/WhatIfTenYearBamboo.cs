using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfTenYearBamboo : WhatIfRelicModel, IWhatIfUniformRelicSource
{
    public WhatIfTenYearBamboo() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        var bambooModel = ModelDb.Relic<TenYearBamboo>();

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                var bambooRelic = bambooModel.ToMutable();
                rewards[i] = new RelicReward(bambooRelic, player);
            }
        }

        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return ModelDb.Relic<TenYearBamboo>();
    }
}
