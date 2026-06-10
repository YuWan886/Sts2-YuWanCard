using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfChemicalX : WhatIfRelicModel, IWhatIfUniformRelicSource
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromRelic<ChemicalX>();

    public WhatIfChemicalX() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        var chemicalXModel = ModelDb.Relic<ChemicalX>();

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                rewards[i] = new RelicReward(chemicalXModel.ToMutable(), player);
            }
        }

        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return ModelDb.Relic<ChemicalX>();
    }
}
