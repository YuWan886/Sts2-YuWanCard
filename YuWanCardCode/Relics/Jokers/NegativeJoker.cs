using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// Gain +1 hand draw per turn.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class NegativeJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (Owner == null || player != Owner)
        {
            return base.ModifyHandDraw(player, count);
        }

        return count + 1;
    }
}
