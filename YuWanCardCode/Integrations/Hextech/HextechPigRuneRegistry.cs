using MegaCrit.Sts2.Core.Models;
using YuWanCard.Relics;

namespace YuWanCard.Hextech;

public static class HextechPigRuneRegistry
{
    private static readonly IReadOnlyList<Type> SilverRunes =
    [
        typeof(PigletDashRune),
        typeof(PigletGuardRune),
        typeof(GluttonsFeastRune),
        typeof(ToughPigskinRune),
        typeof(PigletRechargeRune),
        typeof(ShareTheFoodRune)
    ];

    private static readonly IReadOnlyList<Type> GoldRunes =
    [
        typeof(PigBreederRune),
        typeof(EndlessBuffetRune),
        typeof(GildedPigskinRune),
        typeof(CoinRainRune),
        typeof(SwornBrotherRune)
    ];

    private static readonly IReadOnlyList<Type> PrismaticRunes =
    [
        typeof(AngelPigletRune),
        typeof(ThroneOfPigsRune),
        typeof(HextechShoppingCartRune),
        typeof(PerpetualPigRune)
    ];

    private static readonly IReadOnlyList<Type> SharedSilverRunes =
    [
        typeof(PiggyBankRune),
        typeof(HeartyMealRune)
    ];

    private static readonly IReadOnlyList<Type> SharedGoldRunes =
    [
        typeof(SinOfGluttonyRune),
        typeof(SinOfSlothRune),
        typeof(SinOfPrideRune),
        typeof(SinOfEnvyRune),
        typeof(SinOfLustRune),
        typeof(SinOfGreedRune),
        typeof(SinOfWrathRune)
    ];

    private static readonly IReadOnlySet<Type> FirstActExcluded = new HashSet<Type>
    {
        typeof(SinOfPrideRune),
        typeof(PerpetualPigRune)
    };

    private static readonly IReadOnlySet<Type> ThirdActExcluded = new HashSet<Type>
    {
        typeof(SinOfWrathRune),
        typeof(PigBreederRune)
    };

    private static readonly IReadOnlySet<Type> SevenSinsRunes = new HashSet<Type>
    {
        typeof(SinOfGluttonyRune),
        typeof(SinOfSlothRune),
        typeof(SinOfPrideRune),
        typeof(SinOfEnvyRune),
        typeof(SinOfLustRune),
        typeof(SinOfGreedRune),
        typeof(SinOfWrathRune)
    };

    public static IReadOnlyList<Type> GetAllRunes()
    {
        return SilverRunes.Concat(GoldRunes).Concat(PrismaticRunes)
            .Concat(SharedSilverRunes).Concat(SharedGoldRunes).ToArray();
    }

    public static IReadOnlyList<Type> GetAllPigRunes()
    {
        return SilverRunes.Concat(GoldRunes).Concat(PrismaticRunes).ToArray();
    }

    public static IReadOnlyList<Type> GetRunesByRarity(HextechRuneRarity rarity)
    {
        return rarity switch
        {
            HextechRuneRarity.Silver => SilverRunes.Concat(SharedSilverRunes).ToArray(),
            HextechRuneRarity.Gold => GoldRunes.Concat(SharedGoldRunes).ToArray(),
            HextechRuneRarity.Prismatic => PrismaticRunes,
            _ => Array.Empty<Type>()
        };
    }

    public static bool IsPigRune(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return GetAllPigRunes().Any(type => ModelDb.GetId(type) == id);
    }

    public static bool IsSharedRune(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return SharedSilverRunes.Concat(SharedGoldRunes).Any(type => ModelDb.GetId(type) == id);
    }

    public static bool IsPigOrSharedRune(RelicModel? relic)
    {
        return IsPigRune(relic) || IsSharedRune(relic);
    }

    public static bool TryGetRarity(RelicModel? relic, out HextechRuneRarity rarity)
    {
        rarity = default;
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        foreach (HextechRuneRarity value in Enum.GetValues<HextechRuneRarity>())
        {
            if (GetRunesByRarity(value).Any(type => ModelDb.GetId(type) == id))
            {
                rarity = value;
                return true;
            }
        }

        return false;
    }

    public static bool IsAllowedInAct(Type runeType, int actIndex)
    {
        return actIndex switch
        {
            0 => !FirstActExcluded.Contains(runeType),
            2 => !ThirdActExcluded.Contains(runeType),
            _ => true
        };
    }

    public static bool IsAvailableForPlayer(RelicModel relic, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (IsSharedRune(relic))
        {
            return true;
        }

        return player.Character.Id == ModelDb.GetId<Characters.Pig>();
    }

    public static string GetPoolKey(RelicModel relic)
    {
        if (IsSharedRune(relic))
        {
            return HextechRunePoolKey.Generic;
        }

        return IsPigRune(relic) ? HextechRunePoolKey.Pig : HextechRunePoolKey.Generic;
    }

    public static IReadOnlySet<ModelId> GetMutuallyExclusiveRuneIds(IEnumerable<ModelId> ownedIds)
    {
        HashSet<ModelId> ownedSet = ownedIds.ToHashSet();
        HashSet<ModelId> blocked = [];

        int ownedSevenSins = SevenSinsRunes.Count(type => ownedSet.Contains(ModelDb.GetId(type)));
        if (ownedSevenSins >= 2)
        {
            blocked.UnionWith(SevenSinsRunes
                .Select(ModelDb.GetId)
                .Where(id => !ownedSet.Contains(id)));
        }

        AddMutualBlock<EndlessBuffetRune, SinOfGluttonyRune>(ownedSet, blocked);
        AddMutualBlock<CoinRainRune, SinOfGreedRune>(ownedSet, blocked);
        AddMutualBlock<SinOfPrideRune, SinOfWrathRune>(ownedSet, blocked);
        AddMutualBlock<SinOfEnvyRune, SinOfLustRune>(ownedSet, blocked);
        AddMutualBlock<ThroneOfPigsRune, SwornBrotherRune>(ownedSet, blocked);

        return blocked;
    }

    private static void AddMutualBlock<TRuneA, TRuneB>(HashSet<ModelId> ownedSet, HashSet<ModelId> blocked)
        where TRuneA : AbstractModel
        where TRuneB : AbstractModel
    {
        ModelId idA = ModelDb.GetId<TRuneA>();
        ModelId idB = ModelDb.GetId<TRuneB>();
        if (ownedSet.Contains(idA) && !ownedSet.Contains(idB))
        {
            blocked.Add(idB);
        }

        if (ownedSet.Contains(idB) && !ownedSet.Contains(idA))
        {
            blocked.Add(idA);
        }
    }
}
