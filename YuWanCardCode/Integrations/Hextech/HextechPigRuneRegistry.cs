using MegaCrit.Sts2.Core.Models;
using YuWanCard.Relics;

namespace YuWanCard.Hextech;

/// <summary>
/// Declarative table of every pig rune: rarity, pool ownership and act flags.
/// <see cref="HextechRuneInteropBridge"/> feeds it straight into HextechRunes' external
/// registration API, so Hextech's pool building, config menu and compendium all see the
/// runes with the same metadata.
/// </summary>
public static class HextechPigRuneRegistry
{
    /// <param name="IsShared">Shared runes are offered to every character; pig runes are Pig-only.</param>
    public readonly record struct HextechRuneDefinition(
        Type RuneType,
        HextechRuneRarity Rarity,
        bool IsShared = false,
        bool FirstActExcluded = false,
        bool ThirdActExcluded = false)
    {
        /// <summary>Rarity name as HextechRunesInterop expects it (HextechRarityTier member).</summary>
        public string RarityName => Rarity.ToString();

        /// <summary>Comma-separated PlayerRuneFlags names, or null when no flag applies.</summary>
        public string? FlagsName
        {
            get
            {
                List<string> flags = [];
                if (FirstActExcluded)
                {
                    flags.Add("FirstActExcluded");
                }

                if (ThirdActExcluded)
                {
                    flags.Add("ThirdActExcluded");
                }

                return flags.Count == 0 ? null : string.Join(",", flags);
            }
        }

        /// <summary>HEXTECH_POOL.&lt;key&gt; source label shown on the selection screen.</summary>
        public string PoolKey => IsShared ? HextechRunePoolKey.Generic : HextechRunePoolKey.Pig;
    }

    private static readonly IReadOnlyList<HextechRuneDefinition> Definitions =
    [
        new(typeof(PigletDashRune), HextechRuneRarity.Silver),
        new(typeof(PigletGuardRune), HextechRuneRarity.Silver),
        new(typeof(GluttonsFeastRune), HextechRuneRarity.Silver),
        new(typeof(ToughPigskinRune), HextechRuneRarity.Silver),
        new(typeof(PigletRechargeRune), HextechRuneRarity.Silver),
        new(typeof(ShareTheFoodRune), HextechRuneRarity.Silver),

        new(typeof(PigBreederRune), HextechRuneRarity.Gold, ThirdActExcluded: true),
        new(typeof(EndlessBuffetRune), HextechRuneRarity.Gold),
        new(typeof(GildedPigskinRune), HextechRuneRarity.Gold),
        new(typeof(CoinRainRune), HextechRuneRarity.Gold),
        new(typeof(SwornBrotherRune), HextechRuneRarity.Gold),

        new(typeof(AngelPigletRune), HextechRuneRarity.Prismatic),
        new(typeof(ThroneOfPigsRune), HextechRuneRarity.Prismatic),
        new(typeof(HextechShoppingCartRune), HextechRuneRarity.Prismatic),
        new(typeof(PerpetualPigRune), HextechRuneRarity.Prismatic, FirstActExcluded: true),

        new(typeof(SavingsAccountRune), HextechRuneRarity.Silver, IsShared: true),
        new(typeof(HeartyMealRune), HextechRuneRarity.Silver, IsShared: true),

        new(typeof(SinOfGluttonyRune), HextechRuneRarity.Gold, IsShared: true),
        new(typeof(SinOfSlothRune), HextechRuneRarity.Gold, IsShared: true),
        new(typeof(SinOfPrideRune), HextechRuneRarity.Gold, IsShared: true, FirstActExcluded: true),
        new(typeof(SinOfEnvyRune), HextechRuneRarity.Gold, IsShared: true),
        new(typeof(SinOfLustRune), HextechRuneRarity.Gold, IsShared: true),
        new(typeof(SinOfGreedRune), HextechRuneRarity.Gold, IsShared: true),
        new(typeof(SinOfWrathRune), HextechRuneRarity.Gold, IsShared: true, ThirdActExcluded: true)
    ];

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

    public static IReadOnlyList<HextechRuneDefinition> GetDefinitions() => Definitions;

    public static IReadOnlyList<Type> GetAllPigRunes()
    {
        return Definitions.Where(static definition => !definition.IsShared)
            .Select(static definition => definition.RuneType)
            .ToArray();
    }

    public static bool IsPigOrSharedRune(RelicModel? relic)
    {
        return TryGetDefinition(relic, out _);
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

    private static bool TryGetDefinition(RelicModel? relic, out HextechRuneDefinition definition)
    {
        definition = default;
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        foreach (HextechRuneDefinition candidate in Definitions)
        {
            if (ModelDb.GetId(candidate.RuneType) == id)
            {
                definition = candidate;
                return true;
            }
        }

        return false;
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
