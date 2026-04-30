using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Core.Patches.UI;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanCharacterModel : CharacterModel, IYuWanCharacter
{
    public virtual string? CustomIconTexturePath => null;
    public virtual string? CustomCharacterSelectIconPath => null;
    public virtual string? CustomEnergyCounterPath => null;
    public virtual string? CustomCharacterSelectLockedIconPath => null;
    public virtual string? CustomVisualPath => null;
    public virtual string? CustomTrailPath => null;
    public virtual string? CustomIconPath => null;
    public virtual string? CustomIconOutlineTexturePath => null;
    public virtual string? CustomRestSiteAnimPath => null;
    public virtual string? CustomMerchantAnimPath => null;
    public virtual string? CustomArmPointingTexturePath => null;
    public virtual string? CustomArmRockTexturePath => null;
    public virtual string? CustomArmPaperTexturePath => null;
    public virtual string? CustomArmScissorsTexturePath => null;
    public virtual string? CustomCharacterSelectBg => null;
    public virtual string? CustomCharacterSelectTransitionPath => null;
    public virtual string? CustomMapMarkerPath => null;
    public virtual string? CustomAttackSfx => null;
    public virtual string? CustomCastSfx => null;
    public virtual string? CustomDeathSfx => null;
    public virtual RelicIconData? CustomYummyCookie => null;

    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null;

    string? IYuWanCharacter.CustomIconTexturePath => CustomIconTexturePath;
    string? IYuWanCharacter.CustomCharacterSelectIconPath => CustomCharacterSelectIconPath;
    string? IYuWanCharacter.CustomEnergyCounterPath => CustomEnergyCounterPath;
    string? IYuWanCharacter.CustomCharacterSelectLockedIconPath => CustomCharacterSelectLockedIconPath;
    string? IYuWanCharacter.CustomVisualPath => CustomVisualPath;
    string? IYuWanCharacter.CustomTrailPath => CustomTrailPath;
    string? IYuWanCharacter.CustomIconPath => CustomIconPath;
    string? IYuWanCharacter.CustomIconOutlineTexturePath => CustomIconOutlineTexturePath;
    string? IYuWanCharacter.CustomRestSiteAnimPath => CustomRestSiteAnimPath;
    string? IYuWanCharacter.CustomMerchantAnimPath => CustomMerchantAnimPath;
    string? IYuWanCharacter.CustomArmPointingTexturePath => CustomArmPointingTexturePath;
    string? IYuWanCharacter.CustomArmRockTexturePath => CustomArmRockTexturePath;
    string? IYuWanCharacter.CustomArmPaperTexturePath => CustomArmPaperTexturePath;
    string? IYuWanCharacter.CustomArmScissorsTexturePath => CustomArmScissorsTexturePath;
    string? IYuWanCharacter.CustomCharacterSelectBg => CustomCharacterSelectBg;
    string? IYuWanCharacter.CustomCharacterSelectTransitionPath => CustomCharacterSelectTransitionPath;
    string? IYuWanCharacter.CustomMapMarkerPath => CustomMapMarkerPath;
    string? IYuWanCharacter.CustomAttackSfx => CustomAttackSfx;
    string? IYuWanCharacter.CustomCastSfx => CustomCastSfx;
    string? IYuWanCharacter.CustomDeathSfx => CustomDeathSfx;
    Control? IYuWanCharacter.CustomIcon => null;
    NCreatureVisuals? IYuWanCharacter.CreateCustomVisuals() => null;
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
