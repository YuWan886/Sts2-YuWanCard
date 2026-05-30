using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class KillerAuraTrait : MaliceTraitPowerBase
{
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        // Only affect players' cards (not the trait owner's side)
        if (card.Owner.Creature == Owner || card.Owner.Creature?.Side == Owner.Side)
        {
            return false;
        }

        if (card.Type != CardType.Attack)
        {
            return false;
        }

        modifiedCost = originalCost + 1;
        return true;
    }
}
