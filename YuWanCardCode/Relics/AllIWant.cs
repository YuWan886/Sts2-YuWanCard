using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class AllIWant : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public AllIWant() : base(true)
    {
    }

    public override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward)
    {
        return player == Owner && cardReward.Cards.Any();
    }
}
