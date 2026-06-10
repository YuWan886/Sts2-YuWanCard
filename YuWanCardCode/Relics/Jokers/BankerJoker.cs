using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class BankerJoker : BalatroJokerRelicModel
{
    private const int BonusGoldPerPayout = 3;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public int GetInterestBonusGold()
    {
        return BonusGoldPerPayout * EffectiveCount();
    }
}
