using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Characters;

public class PigCardPool : CustomCardPoolModel
{
    public override string Title => "yuwan_pig";
    
    public override string EnergyColorName => "ironclad";
    
    public override string CardFrameMaterialPath => "card_frame_red";
    
    public override bool IsShared => false;
    
    public override bool IsColorless => false;
    
    public override Color DeckEntryCardColor => new("FAFAD2");
    
    public override Color EnergyOutlineColor => new("FFFF00");
    
    protected override CardModel[] GenerateAllCards()
    {
        return
        [
            ModelDb.Card<Cards.PigStrike>(),
            ModelDb.Card<Cards.PigDefend>()
        ];
    }
}
