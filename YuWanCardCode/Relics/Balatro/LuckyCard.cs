using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// Every 7th card played triggers +2 extra times.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class LuckyCard : BalatroRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        int result = base.ModifyCardPlayCount(card, target, playCount);

        if (Owner == null || card.Owner != Owner)
        {
            return result;
        }

        BalatroModifier? modifier = GetModifier();
        if (modifier != null && (modifier.CardsPlayedThisTurn + 1) % 7 == 0)
        {
            result += 2;
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
