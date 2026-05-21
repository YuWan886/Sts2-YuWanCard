using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace YuWanCard.Core.Utils;

// --- AncientOption ---

public abstract class AncientOption(int weight) : IWeighted
{
    public int Weight { get; } = weight;

    public abstract IEnumerable<RelicModel> AllVariants { get; }
    public abstract RelicModel ModelForOption { get; }

    public static explicit operator AncientOption(RelicModel model) => new BasicAncientOption(model, 1);

    private class BasicAncientOption(RelicModel model, int weight) : AncientOption(weight)
    {
        public override IEnumerable<RelicModel> AllVariants { get; } = [model.ToMutable()];
        public override RelicModel ModelForOption => model.ToMutable();
    }
}

public class AncientOption<T>(int weight) : AncientOption(weight) where T : RelicModel
{
    public Func<T, RelicModel>? ModelPrep { get; init; }
    public Func<T, IEnumerable<RelicModel>>? Variants { get; init; }

    private readonly T _model = ModelDb.Relic<T>();

    public override IEnumerable<RelicModel> AllVariants => Variants == null ? [_model.ToMutable()] : Variants(_model);
    public override RelicModel ModelForOption => ModelPrep == null ? _model.ToMutable() : ModelPrep(_model.ToMutable() as T ?? _model);
}

// --- OptionPools ---

public class OptionPools
{
    private WeightedList<AncientOption>[] _pools;

    public OptionPools(WeightedList<AncientOption> pool1, WeightedList<AncientOption> pool2, WeightedList<AncientOption> pool3)
    {
        _pools = [pool1, pool2, pool3];
    }

    public OptionPools(WeightedList<AncientOption> pool12, WeightedList<AncientOption> pool3)
    {
        _pools = [pool12, pool12, pool3];
    }

    public OptionPools(WeightedList<AncientOption> pool)
    {
        _pools = [pool, pool, pool];
    }

    public IEnumerable<AncientOption> AllOptions => _pools.SelectMany(pool => pool);

    public List<AncientOption> Roll(Rng rng)
    {
        List<AncientOption> result = [];

        var pool = _pools[0];
        WeightedList<AncientOption> rollPool = [.. pool];
        result.Add(rollPool.GetRandom(rng, true));

        if (pool != _pools[1])
        {
            pool = _pools[1];
            rollPool = [.. pool];
        }
        result.Add(rollPool.GetRandom(rng, true));

        if (pool != _pools[2])
        {
            pool = _pools[2];
            rollPool = [.. pool];
        }
        result.Add(rollPool.GetRandom(rng, true));

        return result;
    }
}
