using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfThreeEggs : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public WhatIfThreeEggs() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        RelicModel[] eggRelics =
        [
            ModelDb.Relic<MoltenEgg>(),
            ModelDb.Relic<ToxicEgg>(),
            ModelDb.Relic<FrozenEgg>()
        ];

        foreach (RelicModel eggRelic in eggRelics)
        {
            if (Owner.GetRelicById(eggRelic.Id) != null)
            {
                continue;
            }

            await RelicCmd.Obtain(eggRelic.ToMutable(), Owner);
        }
    }
}
