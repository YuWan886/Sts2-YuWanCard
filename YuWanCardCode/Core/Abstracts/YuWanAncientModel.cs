using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Patches;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanAncientModel : AncientEventModel, IYuWanContent
{
    protected YuWanAncientModel()
    {
        CustomAncientRegistry.Register(this);
    }

    public virtual bool IsValidForAct(ActModel act) => true;
    public virtual bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    protected abstract OptionPools MakeOptionPools { get; }

    private OptionPools? _optionPools;
    public OptionPools OptionPools
    {
        get
        {
            if (_optionPools == null) _optionPools = MakeOptionPools;
            return _optionPools;
        }
    }

    public virtual string? CustomScenePath => null;
    public virtual string? CustomMapIconPath => null;
    public virtual string? CustomMapIconOutlinePath => null;
    public virtual string? CustomRunHistoryIconPath => null;
    public virtual string? CustomRunHistoryIconOutlinePath => null;

    public override IEnumerable<EventOption> AllPossibleOptions =>
        OptionPools.AllOptions.SelectMany(option => option.AllVariants.Select(relic => RelicOption(relic)));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        OptionPools.Roll(Rng).Select(option => RelicOption(option.ModelForOption)).ToList();

    public static WeightedList<AncientOption> MakePool(params RelicModel[] options)
    {
        WeightedList<AncientOption> pool = [.. options.Select(model => (AncientOption)model)];
        return pool;
    }

    public static WeightedList<AncientOption> MakePool(params AncientOption[] options)
    {
        WeightedList<AncientOption> pool = [.. options];
        return pool;
    }
}
