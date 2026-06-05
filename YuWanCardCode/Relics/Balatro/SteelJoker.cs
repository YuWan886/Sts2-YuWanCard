using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// At turn end, retain 20% combo (overrides the default 10%). Effect in BalatroModifier.AfterTurnEnd.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class SteelJoker : BalatroRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
