using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Malice;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(SharedRelicPool))]
public sealed class WrathMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public WrathMalice() : base(true)
    {
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner?.Creature || target == null || !MaliceHelper.IsTraitEnemy(target))
        {
            return 1m;
        }

        return 1.50m;
    }
}
