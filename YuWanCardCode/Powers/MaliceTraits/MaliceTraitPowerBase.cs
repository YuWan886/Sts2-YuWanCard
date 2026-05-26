using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers.MaliceTraits;

public abstract class MaliceTraitPowerBase : YuWanPowerModel
{
    public sealed override PowerType Type => PowerType.Buff;
    public sealed override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// DynamicVar names that should be auto-updated to (initialValue × Amount) when the power amount changes.
    /// </summary>
    protected virtual string[] AutoUpdateVarNames => [];

    private Dictionary<string, decimal>? _varMultipliers;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        InitVarMultipliers();
        UpdateSmartVars();
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(power, amount, applier, cardSource);
        UpdateSmartVars();
    }

    private void InitVarMultipliers()
    {
        if (_varMultipliers != null) return;
        _varMultipliers = new Dictionary<string, decimal>();
        foreach (var name in AutoUpdateVarNames)
        {
            _varMultipliers[name] = DynamicVars[name].BaseValue;
        }
    }

    private void UpdateSmartVars()
    {
        if (_varMultipliers == null) return;
        foreach (var kvp in _varMultipliers)
        {
            DynamicVars[kvp.Key].BaseValue = kvp.Value * Amount;
        }
    }
}
