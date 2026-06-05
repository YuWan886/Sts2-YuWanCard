using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// For each 0-cost card in hand, attack cards deal +1 damage.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class MiserJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner.Creature || cardSource?.Type != CardType.Attack)
        {
            return 0m;
        }

        int multiplier = EffectiveCount();
        if (multiplier <= 0)
        {
            return 0m;
        }

        int zeroCostCount = PileType.Hand.GetPile(Owner).Cards
            .Count(card => !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0);
        return zeroCostCount * multiplier;
    }

    private int EffectiveCount()
    {
        int count = 1;
        if (Owner != null && Owner.GetRelic<Blueprint>() != null)
        {
            count *= 2;
        }
        return count;
    }
}
