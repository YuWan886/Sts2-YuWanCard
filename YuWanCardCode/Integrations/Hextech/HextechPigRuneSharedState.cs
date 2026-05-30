using MegaCrit.Sts2.Core.Models;
using YuWanCard.Relics;

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
        return relic.Owner?.RunState?.Rng?.Niche.NextFloat() < chance;
    }
}
