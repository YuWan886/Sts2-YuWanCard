using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Characters;
using YuWanCard.Config;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

/// <summary>
/// 死亡特效系统 - 当猪角色击杀生物时播放猪死亡动画
/// </summary>
[HarmonyPatch]
public static class DeathEffectPatch
{
    private const string EffectScenePath = "res://YuWanCard/scenes/vfx/pig_death_effect.tscn";

    [HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
    public static class NCreatureStartDeathAnimPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCreature __instance, bool shouldRemove)
        {
            if (__instance == null)
                return;

            if (!YuWanCardConfig.EnableDeathEffect)
                return;

            Creature? creature = __instance.Entity;
            if (creature == null)
                return;

            if (!WasKilledByPig(creature))
                return;

            PlayDeathEffect(__instance, creature);
        }
    }

    private static bool WasKilledByPig(Creature creature)
    {
        try
        {
            var history = CombatManager.Instance?.History;
            if (history == null)
                return false;

            var lastDamageEntry = history.Entries
                .OfType<DamageReceivedEntry>()
                .Where(e => e.Receiver == creature && e.Dealer != null)
                .OrderByDescending(e => e.RoundNumber)
                .FirstOrDefault();

            if (lastDamageEntry?.Dealer == null)
                return false;

            var dealer = lastDamageEntry.Dealer;
            if (!dealer.IsPlayer)
                return false;

            return dealer.Player?.Character is Pig;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"检查击杀者时出错: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 播放死亡特效
    /// </summary>
    private static void PlayDeathEffect(NCreature nCreature, Creature creature)
    {
        try
        {
            var parent = nCreature.GetParent();
            var creatureIndex = nCreature.GetIndex();
            var effectPosition = nCreature.GlobalPosition with { Y = nCreature.GlobalPosition.Y - 50f };

            var effectNode = VfxUtils.PlayAtParent(EffectScenePath, parent, effectPosition, creatureIndex + 1);
            if (effectNode == null)
                return;

            effectNode.TryExecuteOnNode<AnimatedSprite2D>("AnimatedSprite2D",
                sprite =>
                {
                    sprite.Scale = new Vector2(0.4f, 0.4f);
                    sprite.Play();
                });

            effectNode.AddChild(new DeathEffectAutoDestroy());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"播放死亡特效时出错: {ex.Message}");
        }
    }
}

/// <summary>
/// 死亡特效自动销毁控制器 - 显示固定时间后销毁节点
/// </summary>
public partial class DeathEffectAutoDestroy : Node
{
    private double _elapsedTime = 0;
    private const double Duration = 1.5;

    public override void _Process(double delta)
    {
        base._Process(delta);

        _elapsedTime += delta;

        if (_elapsedTime >= Duration)
        {
            var parent = GetParent();
            if (parent != null)
            {
                parent.QueueFree();
            }
            else
            {
                QueueFree();
            }
        }
    }
}
