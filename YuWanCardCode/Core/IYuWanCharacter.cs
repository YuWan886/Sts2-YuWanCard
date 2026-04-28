using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YuWanCard.Core;

/// <summary>
/// Interface for custom character content. Harmony patches check for this
/// to override game-native character resource paths.
/// </summary>
public interface IYuWanCharacter : IYuWanContent
{
    string? CustomVisualPath => null;
    string? CustomTrailPath => "res://scenes/vfx/card_trail_ironclad.tscn";
    string? CustomIconTexturePath => null;
    string? CustomIconOutlineTexturePath => null;
    string? CustomIconPath => null;
    Control? CustomIcon => null;
    string? CustomEnergyCounterPath => null;
    string? CustomRestSiteAnimPath => null;
    string? CustomMerchantAnimPath => null;
    string? CustomArmPointingTexturePath => null;
    string? CustomArmRockTexturePath => null;
    string? CustomArmPaperTexturePath => null;
    string? CustomArmScissorsTexturePath => null;
    string? CustomCharacterSelectBg => null;
    string? CustomCharacterSelectIconPath => null;
    string? CustomCharacterSelectLockedIconPath => "res://images/packed/character_select/char_select_ironclad_locked.png";
    string? CustomCharacterSelectTransitionPath => "res://materials/transitions/ironclad_transition_mat.tres";
    string? CustomMapMarkerPath => "res://images/packed/map/icons/map_marker_ironclad.png";
    string? CustomAttackSfx => "event:/sfx/characters/ironclad/ironclad_attack";
    string? CustomCastSfx => "event:/sfx/characters/ironclad/ironclad_cast";
    string? CustomDeathSfx => "event:/sfx/characters/ironclad/ironclad_die";

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
