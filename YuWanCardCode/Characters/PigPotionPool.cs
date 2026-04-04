using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Characters;

public class PigPotionPool : CustomPotionPoolModel
{
    public override bool IsShared => false;
    
    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return [];
    }
}
