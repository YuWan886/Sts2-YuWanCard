using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfHeartsteel : WhatIfRelicModel, IWhatIfUniformRelicSource
{
    public WhatIfHeartsteel() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        var heartsteelModel = ModelDb.Relic<Heartsteel>();

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                var heartsteelRelic = heartsteelModel.ToMutable();
                rewards[i] = new RelicReward(heartsteelRelic, player);
            }
        }

        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return ModelDb.Relic<Heartsteel>();
    }
}
