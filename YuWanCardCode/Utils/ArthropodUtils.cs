using System;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace YuWanCard.Utils;

public static class ArthropodUtils
{
    private static readonly Type[] ArthropodMonsterTypes =
    [
        typeof(DecimillipedeSegment),
        typeof(DecimillipedeSegmentFront),
        typeof(DecimillipedeSegmentMiddle),
        typeof(DecimillipedeSegmentBack),
        typeof(FuzzyWurmCrawler),
        typeof(BowlbugSilk),
        typeof(Chomper),
        typeof(Entomancer),
        typeof(HunterKiller),
        typeof(Myte),
        typeof(SpinyToad),
        typeof(Tunneler),
        typeof(TheObscura),
        typeof(InfestedPrism),
        typeof(KnowledgeDemon),
        typeof(Crusher),
        typeof(Rocket),
        typeof(SlumberingBeetle),
        typeof(ShrinkerBeetle),
        typeof(FrogKnight),
    ];

    public static bool IsArthropod(Creature creature)
    {
        if (creature == null || !creature.IsMonster)
        {
            return false;
        }

        var monster = creature.Monster;
        if (monster == null)
        {
            return false;
        }

        return IsArthropod(monster);
    }

    public static bool IsArthropod(MonsterModel monster)
    {
        if (monster == null)
        {
            return false;
        }

        if (monster.TakeDamageSfxType == DamageSfxType.Insect)
        {
            return true;
        }

        var monsterType = monster.GetType();
        foreach (var arthropodType in ArthropodMonsterTypes)
        {
            if (arthropodType.IsAssignableFrom(monsterType))
            {
                return true;
            }
        }

        return false;
    }
}
