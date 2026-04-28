using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Core.Utils;

namespace YuWanCard.Core.Abstracts;

public abstract partial class YuWanMonsterModel : MonsterModel, IYuWanContent
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string MonsterId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string VisualsBasePath => $"res://YuWanCard/scenes/monsters/{MonsterId}_visuals";

    public virtual string? CustomVisualPath => $"{VisualsBasePath}.tscn";

    public virtual string? CustomAttackSfx => null;
    public virtual string? CustomCastSfx => null;
    public virtual string? CustomDeathSfx => null;

    public virtual NCreatureVisuals? CreateCustomVisuals()
    {
        if (CustomVisualPath == null) return null;
        NodeFactory.RegisterSceneType<NCreatureVisuals>(CustomVisualPath);
        return NodeFactory<NCreatureVisuals>.CreateFromScene(CustomVisualPath);
    }

    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return null;
    }

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

    public static string GenerateMonsterId<T>() where T : YuWanMonsterModel
    {
        return CamelCaseRegex.Replace(typeof(T).Name, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateVisualsPath<T>() where T : YuWanMonsterModel
    {
        var monsterId = GenerateMonsterId<T>();
        return $"res://YuWanCard/scenes/monsters/{monsterId}_visuals.tscn";
    }
}
