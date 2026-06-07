using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class LegendJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    /// <summary>
    /// Returns the bonus multiplier: cardsPlayedThisTurn * 0.2 * effectiveCount.
    /// Called by BalatroModifier.ComboMultiplier.
    /// </summary>
    public float GetLegendBonus()
    {
        if (Owner == null)
        {
            return 0f;
        }

        BalatroModifier? modifier = GetModifier();
        if (modifier == null)
        {
            return 0f;
        }

        return modifier.CardsPlayedThisTurn * 0.2f * EffectiveCount();
    }

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
