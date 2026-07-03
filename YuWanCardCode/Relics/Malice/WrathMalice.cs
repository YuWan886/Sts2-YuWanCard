using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Malice;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(MaliceRelicPool))]
public sealed class WrathMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public WrathMalice() : base(true)
    {
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner?.Creature || target == null || !MaliceHelper.IsTraitEnemy(target))
        {
            return 1m;
        }

        return 1.6m;
    }
}
