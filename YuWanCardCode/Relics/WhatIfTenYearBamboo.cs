using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfTenYearBamboo : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

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
}
