using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YuWanCard.Core;

/// <summary>
/// Interface for custom character content. Harmony patches check for this
/// to override game-native character resource paths.
/// </summary>
public interface IYuWanCharacter : IYuWanContent
{
    string PlaceholderID => "ironclad";

    IReadOnlyList<RelicModel> MultiplayerStartingRelics => [];

    string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + PlaceholderID);
    string? CustomTrailPath => SceneHelper.GetScenePath("vfx/card_trail_" + PlaceholderID);
    string? CustomIconTexturePath => ImageHelper.GetImagePath("ui/top_panel/character_icon_" + PlaceholderID + ".png");
    string? CustomIconOutlineTexturePath => null;
    string? CustomIconPath => SceneHelper.GetScenePath("ui/character_icons/" + PlaceholderID + "_icon");
    Control? CustomIcon => null;
    string? CustomEnergyCounterPath => SceneHelper.GetScenePath("combat/energy_counters/" + PlaceholderID + "_energy_counter");
    string? CustomRestSiteAnimPath => SceneHelper.GetScenePath("rest_site/characters/" + PlaceholderID + "_rest_site");
    string? CustomMerchantAnimPath => SceneHelper.GetScenePath("merchant/characters/" + PlaceholderID + "_merchant");
    string? CustomArmPointingTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_point.png");
    string? CustomArmRockTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_rock.png");
    string? CustomArmPaperTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_paper.png");
    string? CustomArmScissorsTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_scissors.png");
    string? CustomCharacterSelectBg => SceneHelper.GetScenePath("screens/char_select/char_select_bg_" + PlaceholderID);
    string? CustomCharacterSelectIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + PlaceholderID + ".png");
    string? CustomCharacterSelectLockedIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + PlaceholderID + "_locked.png");
    string? CustomCharacterSelectTransitionPath => "res://materials/transitions/" + PlaceholderID + "_transition_mat.tres";
    string? CustomMapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_" + PlaceholderID + ".png");
    string? CustomAttackSfx => $"event:/sfx/characters/{PlaceholderID}/{PlaceholderID}_attack";
    string? CustomCastSfx => $"event:/sfx/characters/{PlaceholderID}/{PlaceholderID}_cast";
    string? CustomDeathSfx => $"event:/sfx/characters/{PlaceholderID}/{PlaceholderID}_die";

    float AttackAnimDelay => 0.15f;
    float CastAnimDelay => 0.25f;
    CharacterModel? UnlocksAfterRunAs => null;

    bool HideFromVanillaCharacterSelect => false;
    bool AllowInVanillaRandomCharacterSelect => true;

    NCreatureVisuals? CreateCustomVisuals() => null;
    CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null;

    protected static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName,
        string? deadName = null, bool deadLoop = false,
        string? hitName = null, bool hitLoop = false,
        string? attackName = null, bool attackLoop = false,
        string? castName = null, bool castLoop = false,
        string? relaxedName = null, bool relaxedLoop = true)
    {
        var idleAnim = new AnimState(idleName, true);
        var deadAnim = deadName == null ? idleAnim : new AnimState(deadName, deadLoop);
        var hitAnim = hitName == null ? idleAnim :
            new AnimState(hitName, hitLoop) { NextState = idleAnim };
        var attackAnim = attackName == null ? idleAnim :
            new AnimState(attackName, attackLoop) { NextState = idleAnim };
        var castAnim = castName == null ? idleAnim :
            new AnimState(castName, castLoop) { NextState = idleAnim };

        AnimState relaxed;
        if (relaxedName == null)
            relaxed = idleAnim;
        else
        {
            relaxed = new AnimState(relaxedName, relaxedLoop);
            relaxed.AddBranch("Idle", idleAnim);
        }

        var animator = new CreatureAnimator(idleAnim, controller);
        animator.AddAnyState("Idle", idleAnim);
        animator.AddAnyState("Dead", deadAnim);
        animator.AddAnyState("Hit", hitAnim);
        animator.AddAnyState("Attack", attackAnim);
        animator.AddAnyState("Cast", castAnim);
        animator.AddAnyState("Relaxed", relaxed);

        return animator;
    }
}
