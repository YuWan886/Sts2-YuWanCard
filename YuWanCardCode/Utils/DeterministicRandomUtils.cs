using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace YuWanCard.Utils;

internal static class DeterministicRandomUtils
{
    public static T? PickStableRandom<T>(IEnumerable<T> source, Rng rng) where T : IComparable<T>
    {
        List<T> items = source.ToList();
        if (items.Count == 0)
        {
            return default;
        }

        items.StableShuffle(rng);
        return items[0];
    }

    public static List<T> TakeStableRandom<T>(IEnumerable<T> source, int count, Rng rng) where T : IComparable<T>
    {
        if (count <= 0)
        {
            return [];
        }

        List<T> items = source.ToList();
        if (items.Count == 0)
        {
            return [];
        }

        items.StableShuffle(rng);
        if (count >= items.Count)
        {
            return items;
        }

        return items.Take(count).ToList();
    }

    public static bool RollProbability(Rng rng, float chance)
    {
        if (chance <= 0f)
        {
            return false;
        }

        if (chance >= 1f)
        {
            return true;
        }

        return rng.NextFloat() < chance;
    }

    public static int NextInclusive(Rng rng, int minInclusive, int maxInclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minInclusive, maxInclusive);
        return rng.NextInt(minInclusive, maxInclusive + 1);
    }

    public static PowerModel? PickDeterministicBuffPower(IEnumerable<PowerModel> source, Rng rng)
    {
        List<PowerModel> items = source
            .OrderBy(power => power.Id?.Entry, StringComparer.Ordinal)
            .ToList();

        if (items.Count == 0)
        {
            return null;
        }

        return items[rng.NextInt(items.Count)];
    }

    public static RelicModel? PickDeterministicRelic(IEnumerable<RelicModel> source, Rng rng)
    {
        List<RelicModel> items = source
            .OrderBy(relic => relic.Id?.Entry, StringComparer.Ordinal)
            .ToList();

        if (items.Count == 0)
        {
            return null;
        }

        return items[rng.NextInt(items.Count)];
    }
}
