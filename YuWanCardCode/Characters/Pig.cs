using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Cards;
using YuWanCard.Core.Patches.UI;
using YuWanCard.Relics;

namespace YuWanCard.Characters;

public class Pig : CharacterModel, IYuWanCharacter, IYuWanCharacterSkinProvider
{
    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";
    private const string PiggyGirlVisualsPath = "res://YuWanCard/scenes/characters/piggy_girl.tscn";
    private const string PigMerchantPath = "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    private const string PiggyGirlMerchantPath = "res://YuWanCard/scenes/characters/piggy_girl_merchant.tscn";
    private const string PigEnergyCounterPath = "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";
    private const string PigRestSitePath = "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";
    private const string PigTransitionMaterialPath = "res://YuWanCard/materials/transitions/pig_transition_mat.tres";
    private const string PigYummyCookiePath = "res://YuWanCard/images/relics/pig_yummy_cookie.png";
    private const string PigCharacterSelectBgPath = "res://YuWanCard/scenes/characters/char_select_bg_pig.tscn";
    private const string PigCharacterIconPath = "res://YuWanCard/images/characters/character_icon_pig.png";
    private const string PiggyGirlCharacterIconPath = "res://YuWanCard/images/characters/character_icon_piggy_girl.png";
    internal const string PigCharacterSelectSfxPath = "res://YuWanCard/sounds/characters/pig_select.mp3";
    private static readonly IReadOnlyList<YuWanCharacterSkinDefinition> PigSkins =
    [
        new(
            Id: "classic",
            DisplayNameLocKey: "YUWANCARD-CHARACTER_SKIN.PIG_CLASSIC"),
        new(
            Id: "piggy_girl",
            DisplayNameLocKey: "YUWANCARD-CHARACTER_SKIN.PIGGY_GIRL",
            VisualPath: PiggyGirlVisualsPath,
            MerchantAnimPath: PiggyGirlMerchantPath,
            IconTexturePath: PiggyGirlCharacterIconPath,
            IconOutlineTexturePath: PiggyGirlCharacterIconPath)
    ];

    /// <summary>
    /// Registers Pig-specific scene type conversions with NodeFactory
    /// so that Godot PackedScene instances are auto-converted to game types.
    /// </summary>
    public static void RegisterScenes()
    {
        NodeFactory.RegisterSceneType<NCreatureVisuals>(PigVisualsPath);
        NodeFactory.RegisterSceneType<NCreatureVisuals>(PiggyGirlVisualsPath);
        NodeFactory.RegisterSceneType<NMerchantCharacter>(PigMerchantPath);
        NodeFactory.RegisterSceneType<NMerchantCharacter>(PiggyGirlMerchantPath);
        NodeFactory.RegisterSceneType<NEnergyCounter>(PigEnergyCounterPath);
        NodeFactory.RegisterSceneType<NRestSiteCharacter>(PigRestSitePath);
    }

    IReadOnlyList<YuWanCharacterSkinDefinition> IYuWanCharacterSkinProvider.CharacterSkins => PigSkins;

    IReadOnlyList<RelicModel> IYuWanCharacter.MultiplayerStartingRelics => [ModelDb.Relic<PigRoastPork>()];

    string? IYuWanCharacter.CustomVisualPath
        => CharacterSkinSelectionManager.ResolveVisualPath(this, PigVisualsPath);
    string? IYuWanCharacter.CustomEnergyCounterPath => "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";

    NCreatureVisuals? IYuWanCharacter.CreateCustomVisuals()
    {
        string resolvedPath = CharacterSkinSelectionManager.ResolveVisualPath(this, PigVisualsPath);
        return NodeFactory.CreateFromScene<NCreatureVisuals>(resolvedPath);
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

    public override string CharacterSelectSfx => PigCharacterSelectSfxPath;

    string? IYuWanCharacter.CustomCharacterSelectIconPath
        => "res://YuWanCard/images/characters/char_select_pig.png";
    string? IYuWanCharacter.CustomIconPath
        => "res://YuWanCard/scenes/ui/character_icons/pig_icon.tscn";
    Control? IYuWanCharacter.CustomIcon
        => CreateCustomIcon();
    string? IYuWanCharacter.CustomIconTexturePath
        => CharacterSkinSelectionManager.ResolveIconTexturePath(this, PigCharacterIconPath);
    string? IYuWanCharacter.CustomIconOutlineTexturePath
        => CharacterSkinSelectionManager.ResolveIconOutlineTexturePath(this, PigCharacterIconPath);
    string? IYuWanCharacter.CustomCharacterSelectBg
        => PigCharacterSelectBgPath;
    string? IYuWanCharacter.CustomCharacterSelectTransitionPath
        => PigTransitionMaterialPath;
    string? IYuWanCharacter.CustomMerchantAnimPath
        => CharacterSkinSelectionManager.ResolveMerchantAnimPath(this, PigMerchantPath);
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
    RelicIconData? IYuWanCharacter.CustomYummyCookie
        => new(PigYummyCookiePath, PigYummyCookiePath, PigYummyCookiePath);

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigStrike>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigDefend>(),
        ModelDb.Card<PigFriends>(),
        ModelDb.Card<PigShelter>(),
        ModelDb.Card<PigMissYou>()
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

    public static CreatureAnimator CreateCreatureAnimator(MegaSprite controller)
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

        var tf2Anim = new AnimState("tf2", false)
        {
            NextState = new AnimState("idle_loop", true)
        };
        animator.AddAnyState("Tf2", tf2Anim);

        return animator;
    }

    CreatureAnimator? IYuWanCharacter.SetupCustomAnimationStates(MegaSprite controller)
    {
        return CreateCreatureAnimator(controller);
    }

    private Control? CreateCustomIcon()
    {
        PackedScene? iconScene = ResourceLoader.Load<PackedScene>(
            "res://YuWanCard/scenes/ui/character_icons/pig_icon.tscn",
            cacheMode: ResourceLoader.CacheMode.Reuse);
        if (iconScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled) is not TextureRect icon)
        {
            return null;
        }

        string iconPath = CharacterSkinSelectionManager.ResolveIconTexturePath(this, PigCharacterIconPath);
        icon.Texture = ResourceLoader.Load<Texture2D>(iconPath, cacheMode: ResourceLoader.CacheMode.Reuse);
        return icon;
    }
}
