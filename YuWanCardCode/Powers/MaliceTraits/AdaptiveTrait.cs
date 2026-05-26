using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class AdaptiveTrait : MaliceTraitPowerBase
{
    private class Data
    {
        public Creature? LastDealer;
        public string? LastCardId;
    }

    protected override object InitInternalData() => new Data();

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return 1m;
        }

        Data data = GetInternalData<Data>();
        string? currentCardId = cardSource?.Id.Entry;
        bool repeatedSource = dealer == data.LastDealer && currentCardId != null && currentCardId == data.LastCardId;
        data.LastDealer = dealer;
        data.LastCardId = currentCardId;
        return repeatedSource ? 0.7m : 1m;
    }
}
