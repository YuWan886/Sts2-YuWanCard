using BaseLib.Abstracts;

namespace YuWanCard.Characters;

public class PigRelicPool : CustomRelicPoolModel
{
    public override string? TextEnergyIconPath => "res://YuWanCard/images/characters/pig_text_enery.png";

    public override string? BigEnergyIconPath => "res://YuWanCard/images/characters/pig_enery_counter.png";
    
    public override bool IsShared => false;
    
}
