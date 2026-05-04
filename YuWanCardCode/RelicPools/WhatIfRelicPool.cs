using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.RelicPools;

public sealed class WhatIfRelicPool : YuWanRelicPoolModel
{
    public override bool IsShared => true;

    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
