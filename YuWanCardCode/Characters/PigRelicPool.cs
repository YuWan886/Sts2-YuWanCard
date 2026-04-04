using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Characters;

public class PigRelicPool : CustomRelicPoolModel
{
    public override bool IsShared => false;
    
    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            ModelDb.Relic<Relics.PigCarrot>(),
            ModelDb.Relic<Relics.PigGoldenCarrot>()
        ];
    }
}
