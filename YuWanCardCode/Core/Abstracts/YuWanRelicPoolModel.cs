using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Patches;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanRelicPoolModel : RelicPoolModel, IYuWanContent
{
    public override string EnergyColorName => CustomEnergyIconPatches.RegisterPoolEnergyIcon(Id, BigEnergyIconPath, TextEnergyIconPath);
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;
    public virtual bool IsShared => false;

    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
