using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Characters;

public class PigCardPool : CustomCardPoolModel
{
    public override string Title => "yuwan_pig";
    
    public override string EnergyColorName => "ironclad";
    
    public override Color ShaderColor => new("F5C48C");
    
    public override bool IsShared => false;
    
    public override bool IsColorless => false;
    
    public override Color DeckEntryCardColor => new("FAFAD2");
    
    public override Color EnergyOutlineColor => new("A46B2B");
}
