using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Cards;
using YuWanCard.Relics;

namespace YuWanCard.Characters;

public class Pig : CharacterModel, IYuWanCharacter
{
    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";

    IReadOnlyList<RelicModel> IYuWanCharacter.MultiplayerStartingRelics => [ModelDb.Relic<PigRoastPork>()];

    string? IYuWanCharacter.CustomVisualPath => PigVisualsPath;
    string? IYuWanCharacter.CustomEnergyCounterPath => "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";

    NCreatureVisuals? IYuWanCharacter.CreateCustomVisuals()
    {
        return NodeFactory.CreateFromScene<NCreatureVisuals>(PigVisualsPath);
    }

    public override Color NameColor => new("FA8072");
    public override Color EnergyLabelOutlineColor => new("773726");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 80;
    public override int StartingGold => 99;

    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;

    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override CardPoolModel CardPool => ModelDb.CardPool<PigCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<PigRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<PigPotionPool>();

    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

    string? IYuWanCharacter.CustomCharacterSelectIconPath
        => "res://YuWanCard/images/characters/char_select_pig.png";
    string? IYuWanCharacter.CustomIconPath
        => "res://YuWanCard/scenes/ui/character_icons/pig_icon.tscn";
    string? IYuWanCharacter.CustomIconTexturePath
        => "res://YuWanCard/images/characters/character_icon_pig.png";
    string? IYuWanCharacter.CustomIconOutlineTexturePath
        => "res://YuWanCard/images/characters/character_icon_pig.png";
    string? IYuWanCharacter.CustomCharacterSelectBg
        => "res://YuWanCard/scenes/characters/char_select_bg_pig.tscn";
    string? IYuWanCharacter.CustomMerchantAnimPath
        => "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    string? IYuWanCharacter.CustomRestSiteAnimPath
        => "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";
    string? IYuWanCharacter.CustomArmPointingTexturePath
        => "res://YuWanCard/images/characters/multiplayer_hand/pig_point.png";
    string? IYuWanCharacter.CustomArmRockTexturePath
        => "res://images/ui/hands/multiplayer_hand_defect_rock.png";
    string? IYuWanCharacter.CustomArmPaperTexturePath
        => "res://images/ui/hands/multiplayer_hand_defect_paper.png";
    string? IYuWanCharacter.CustomArmScissorsTexturePath
        => "res://images/ui/hands/multiplayer_hand_defect_scissors.png";

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
        ModelDb.Card<PigFriends>(),
        ModelDb.Card<PigShelter>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<PigCarrot>()];

    public override List<string> GetArchitectAttackVfx() =>
    [
        "vfx/vfx_attack_slash",
        "vfx/vfx_bite",
        "vfx/vfx_flying_slash",
        "vfx/vfx_scratch",
        "vfx/vfx_dramatic_stab",
        "vfx/vfx_thrash",
        "vfx/vfx_starry_impact"
    ];

    CreatureAnimator? IYuWanCharacter.SetupCustomAnimationStates(MegaSprite controller)
    {
        var animator = IYuWanCharacter.SetupAnimationState(controller,
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

        var tfAnim = new AnimState("tf", false)
        {
            NextState = new AnimState("idle_loop", true)
        };
        animator.AddAnyState("Tf", tfAnim);

        return animator;
    }
}
