using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// Playing the same card type consecutively grants an extra play.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class MirrorJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        int result = base.ModifyCardPlayCount(card, target, playCount);

        if (Owner == null || card.Owner != Owner)
        {
            return result;
        }

        BalatroModifier? modifier = GetModifier();
        if (modifier?.LastCardTypeThisTurn == card.Type)
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

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
