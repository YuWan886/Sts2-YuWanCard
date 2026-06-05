using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// Status and curse cards count toward combo (status: +0.5, curse: +2). Effect in BalatroModifier.CalculateComboGain.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class WildCard : BalatroRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
