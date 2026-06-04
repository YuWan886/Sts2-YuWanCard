using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// Interest grants +3 extra gold per floor. Effect is applied in BalatroModifier.AfterRoomEntered.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class BankerJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
