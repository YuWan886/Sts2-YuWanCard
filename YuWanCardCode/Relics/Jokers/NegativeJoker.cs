using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class NegativeJoker : BalatroJokerRelicModel
{
    private const float ExtraPlayChance = 0.3f;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        int result = base.ModifyCardPlayCount(card, target, playCount);

        if (Owner == null || card.Owner != Owner)
        {
            return result;
        }

        if (DeterministicRandomUtils.RollProbability(Owner.RunState.Rng.CombatCardSelection, ExtraPlayChance))
        {
            Flash();
            result += EffectiveCount();
        }

        return result;
    }

}
