using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class AdaptiveTrait : MaliceTraitPowerBase
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return 1m;
        }

        string? currentCardId = cardSource?.Id.Entry;
        DamageReceivedEntry? lastDamageTaken = CombatManager.Instance?.History.Entries
            .OfType<DamageReceivedEntry>()
            .LastOrDefault(entry => entry.Receiver == Owner);
        string? lastCardId = lastDamageTaken?.CardSource?.Id.Entry;
        bool repeatedSource = dealer == lastDamageTaken?.Dealer
                              && currentCardId != null
                              && currentCardId == lastCardId;
        return repeatedSource ? 0.7m : 1m;
    }
}
