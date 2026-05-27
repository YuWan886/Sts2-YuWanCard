using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;
using YuWanCard.Hextech;

namespace YuWanCard.Integrations.Hextech.RelicPools;

public sealed class HextechPigRunePool : YuWanRelicPoolModel
{
    public override bool IsShared => true;

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return HextechPigRuneRegistry.GetAllPigRunes()
            .Concat(HextechForgeRegistry.GetAllForges())
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)));
    }
}
