using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.RelicPools;

public sealed class MaliceRelicPool : YuWanRelicPoolModel
{
    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
