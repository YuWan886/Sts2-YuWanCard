using MegaCrit.Sts2.Core.Models;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Hextech;

public static class HextechPigRuneSharedState
{
    public static int ScaleWithRingBonus(RelicModel? relic, int baseValue, int bonusValue)
    {
        if (relic?.Owner == null || relic.Owner.GetRelic<RingOfSevenCurses>() == null)
        {
            return baseValue;
        }

        return baseValue + bonusValue;
    }

    public static bool RollPercent(RelicModel? relic, float baseChance, float ringBonusChance)
    {
        if (relic == null)
        {
            return false;
        }

        float chance = relic.Owner?.GetRelic<RingOfSevenCurses>() == null ? baseChance : baseChance + ringBonusChance;
        return relic.Owner?.RunState?.Rng != null
               && DeterministicRandomUtils.RollProbability(relic.Owner.RunState.Rng.CombatCardSelection, chance);
    }
}
