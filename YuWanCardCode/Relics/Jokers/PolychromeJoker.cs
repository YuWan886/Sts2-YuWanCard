using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Balatro;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class PolychromeJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        int result = base.ModifyCardPlayCount(card, target, playCount);

        if (Owner == null || card.Owner != Owner)
        {
            return result;
        }

        if (BalatroCardEditionHelper.HasEdition(card))
        {
            result += EffectiveCount();
        }

        return result;
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
