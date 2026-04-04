using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;
using YuWanCard.Relics;

namespace YuWanCard.Characters;

public class Pig : PlaceholderCharacterModel
{
    public override string PlaceholderID => "ironclad";
    
    public override string CustomVisualPath => "res://YuWanCard/scenes/characters/pig.tscn";
    
    public override Color NameColor => new("FFDAB9");
    
    public override CharacterGender Gender => CharacterGender.Neutral;
    
    public override int StartingHp => 80;
    
    public override CardPoolModel CardPool => ModelDb.CardPool<PigCardPool>();
    
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<PigRelicPool>();
    
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<PigPotionPool>();
    
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";
    
    public override string? CustomCharacterSelectIconPath 
        => "res://YuWanCard/images/characters/char_select_pig.png";
    
    public override string CustomIconPath 
        => "res://YuWanCard/scenes/ui/character_icons/pig_icon.tscn";
    
    public override string? CustomIconTexturePath 
        => "res://YuWanCard/images/characters/character_icon_pig.png";
    
    public override string CustomCharacterSelectBg 
        => "res://YuWanCard/scenes/characters/char_select_bg_pig.tscn";
    
    public override string CustomMerchantAnimPath 
        => "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    
    public override string CustomRestSiteAnimPath 
        => "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";
    
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigHurt>()
    ];
    
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<PigCarrot>()];
    
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(controller, 
            idleName: "idle_loop",
            deadName: "die",
            deadLoop: false,
            hitName: "hurt",
            hitLoop: false,
            attackName: "attack",
            attackLoop: false,
            castName: "cast",
            castLoop: false,
            relaxedName: "relaxed_loop",
            relaxedLoop: true);
    }
}
