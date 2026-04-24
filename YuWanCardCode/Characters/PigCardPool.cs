using BaseLib.Abstracts;
using Godot;

namespace YuWanCard.Characters;

public class PigCardPool : CustomCardPoolModel
{
    public override string Title => "yuwan_pig";
    
    public override string? TextEnergyIconPath => "res://YuWanCard/images/characters/pig_text_enery.png";

    public override string? BigEnergyIconPath => "res://YuWanCard/images/characters/pig_enery_counter.png";
    
    public override Color ShaderColor => new("F5C48C");
    
    public override bool IsShared => false;
    
    public override bool IsColorless => false;
    
    public override Color DeckEntryCardColor => new("FAFAD2");
    
    public override Color EnergyOutlineColor => new("FF623A");
}
