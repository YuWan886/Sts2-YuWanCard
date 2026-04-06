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
    
    protected override CardModel[] GenerateAllCards()
    {
        return
        [
            ModelDb.Card<Cards.PigStrike>(),
            ModelDb.Card<Cards.PigDefend>(),
            ModelDb.Card<Cards.PigInspire>(),
            ModelDb.Card<Cards.PigShelter>(),
            ModelDb.Card<Cards.PigUnity>(),
            ModelDb.Card<Cards.PigGuard>(),
            ModelDb.Card<Cards.PigFriends>(),
            ModelDb.Card<Cards.PigEncourage>(),
            ModelDb.Card<Cards.PigCurse>(),
            ModelDb.Card<Cards.PigBlessing>(),
            ModelDb.Card<Cards.PigSmash>(),
            ModelDb.Card<Cards.PigTaunt>()
        ];
    }
}
