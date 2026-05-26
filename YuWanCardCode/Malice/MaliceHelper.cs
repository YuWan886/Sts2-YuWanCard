using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Powers;

namespace YuWanCard.Malice;

public static class MaliceHelper
{
    public static int GetMaliceLevel()
    {
        RunState? runState = RunManager.Instance?.State;
        if (runState == null)
        {
            return 0;
        }

        return MaliceModifier.GetMaliceModifier(runState)?.EffectiveMaliceLevel ?? 0;
    }

    public static bool HasMalice(int level) => GetMaliceLevel() >= level;

    public static T GetValueIfMalice<T>(int level, T maliceValue, T fallbackValue) =>
        HasMalice(level) ? maliceValue : fallbackValue;

    public static bool IsTraitEnemy(Creature? creature)
    {
        if (creature == null)
        {
            return false;
        }

        return creature.GetPower<MaliceTraitMarkerPower>() != null;
    }

    public static bool IsEnemyCombat(Creature? creature) =>
        creature is { Side: CombatSide.Enemy, CombatState: not null };
}
