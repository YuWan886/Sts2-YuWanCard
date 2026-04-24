using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Characters;

public class PigPotionPool : CustomPotionPoolModel
{
    public override string? TextEnergyIconPath => "res://YuWanCard/images/characters/pig_text_enery.png";

    public override string? BigEnergyIconPath => "res://YuWanCard/images/characters/pig_enery_counter.png";
    
    public override bool IsShared => false;
    
    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return [];
    }
}
