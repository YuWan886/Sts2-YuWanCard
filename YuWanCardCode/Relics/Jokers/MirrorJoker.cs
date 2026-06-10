using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

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
        if (modifier?.GetLastCardTypeThisTurn(Owner) == card.Type)
        {
            result += EffectiveCount();
        }

        return result;
    }

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
