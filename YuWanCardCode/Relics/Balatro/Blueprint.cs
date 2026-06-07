using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class Blueprint : BalatroRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public bool CopiesJoker(BalatroJokerRelicModel joker)
    {
        return Owner != null && joker.Owner == Owner;
    }
}
