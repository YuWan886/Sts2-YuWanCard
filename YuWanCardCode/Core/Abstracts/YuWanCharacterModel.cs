using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Core.Patches.UI;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanCharacterModel : CharacterModel, IYuWanCharacter
{
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    protected override CharacterModel? UnlocksAfterRunAs => null;

    public virtual string PlaceholderID => "ironclad";

    public virtual IReadOnlyList<RelicModel> MultiplayerStartingRelics => [];
    public virtual string? CustomIconTexturePath => ImageHelper.GetImagePath("ui/top_panel/character_icon_" + PlaceholderID + ".png");
    public virtual string? CustomCharacterSelectIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + PlaceholderID + ".png");
    public virtual string? CustomEnergyCounterPath => SceneHelper.GetScenePath("combat/energy_counters/" + PlaceholderID + "_energy_counter");
    public virtual string? CustomCharacterSelectLockedIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + PlaceholderID + "_locked.png");
    public virtual string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + PlaceholderID);
    public virtual string? CustomTrailPath => SceneHelper.GetScenePath("vfx/card_trail_" + PlaceholderID);
    public virtual string? CustomIconPath => SceneHelper.GetScenePath("ui/character_icons/" + PlaceholderID + "_icon");
    public virtual string? CustomIconOutlineTexturePath => null;
    public virtual string? CustomRestSiteAnimPath => SceneHelper.GetScenePath("rest_site/characters/" + PlaceholderID + "_rest_site");
    public virtual string? CustomMerchantAnimPath => SceneHelper.GetScenePath("merchant/characters/" + PlaceholderID + "_merchant");
    public virtual string? CustomArmPointingTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_point.png");
    public virtual string? CustomArmRockTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_rock.png");
    public virtual string? CustomArmPaperTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_paper.png");
    public virtual string? CustomArmScissorsTexturePath => ImageHelper.GetImagePath("ui/hands/" + PlaceholderID + "_arm_scissors.png");
    public virtual string? CustomCharacterSelectBg => SceneHelper.GetScenePath("screens/char_select/char_select_bg_" + PlaceholderID);
    public virtual string? CustomCharacterSelectTransitionPath => "res://materials/transitions/" + PlaceholderID + "_transition_mat.tres";
    public virtual string? CustomMapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_" + PlaceholderID + ".png");
    public virtual string? CustomAttackSfx => $"event:/sfx/characters/{PlaceholderID}/{PlaceholderID}_attack";
    public virtual string? CustomCastSfx => $"event:/sfx/characters/{PlaceholderID}/{PlaceholderID}_cast";
    public virtual string? CustomDeathSfx => $"event:/sfx/characters/{PlaceholderID}/{PlaceholderID}_die";
    public virtual RelicIconData? CustomYummyCookie => null;

    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null;

    protected YuWanCharacterModel()
    {
        RegisterSceneConversions();
    }

    /// <summary>
    /// Auto-registers scene paths for type conversion, matching BaseLib's
    /// ISceneConversions pattern. This allows modders to use plain Node2D/Control
    /// root nodes in their scenes; the SceneConversionPatch converts them to the
    /// game-expected types at instantiation time.
    /// </summary>
    protected virtual void RegisterSceneConversions()
    {
        if (CustomVisualPath != null)
            NodeFactory.RegisterSceneType<NCreatureVisuals>(CustomVisualPath);
        if (CustomEnergyCounterPath != null)
            NodeFactory.RegisterSceneType<NEnergyCounter>(CustomEnergyCounterPath);
        if (CustomMerchantAnimPath != null)
            NodeFactory.RegisterSceneType<NMerchantCharacter>(CustomMerchantAnimPath);
        if (CustomRestSiteAnimPath != null)
            NodeFactory.RegisterSceneType<NRestSiteCharacter>(CustomRestSiteAnimPath);
    }

    Control? IYuWanCharacter.CustomIcon => null;
    NCreatureVisuals? IYuWanCharacter.CreateCustomVisuals()
    {
        if (CustomVisualPath == null) return null;
        return NodeFactory.CreateFromScene<NCreatureVisuals>(CustomVisualPath);
    }
    CreatureAnimator? IYuWanCharacter.SetupCustomAnimationStates(MegaSprite controller) => SetupCustomAnimationStates(controller);

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
