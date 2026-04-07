using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Patches;

/// <summary>
/// 死亡特效系统 - 当生物被击杀时播放猪死亡动画
/// </summary>
[HarmonyPatch]
public static class DeathEffectPatch
{
    // Godot 场景路径
    private const string EffectScenePath = "res://YuWanCard/scenes/vfx/pig_death_effect.tscn";

    /// <summary>
    /// 在 NCreature.StartDeathAnim 方法后插入死亡特效播放
    /// 这样可以确保在节点被移除之前播放特效
    /// </summary>
    [HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
    public static class NCreatureStartDeathAnimPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCreature __instance, bool shouldRemove)
        {
            if (__instance == null)
                return;

            // 获取生物
            Creature? creature = __instance.Entity;
            if (creature == null)
                return;

            // 播放死亡特效
            PlayDeathEffect(__instance, creature);
        }
    }

    /// <summary>
    /// 播放死亡特效
    /// </summary>
    private static void PlayDeathEffect(NCreature nCreature, Creature creature)
    {
        try
        {
            // 获取生物的全局位置
            Vector2 effectPosition = nCreature.GlobalPosition;

            // 加载并实例化特效场景
            var scene = GD.Load<PackedScene>(EffectScenePath);
            if (scene == null)
            {
                MainFile.Logger.Error($"无法加载死亡特效场景: {EffectScenePath}");
                return;
            }

            var effectNode = scene.Instantiate<Node2D>();
            if (effectNode == null)
            {
                MainFile.Logger.Error("无法实例化死亡特效节点");
                return;
            }

            // 设置特效位置
            effectNode.GlobalPosition = effectPosition;

            // 获取 AnimatedSprite2D 节点并设置缩放为固定的 0.6 倍
            var animatedSprite = effectNode.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite != null)
            {
                animatedSprite.Scale = new Vector2(0.5f, 0.5f);
            }

            // 创建自动销毁控制器
            var autoDestroy = new DeathEffectAutoDestroy();
            effectNode.AddChild(autoDestroy);

            // 将特效添加到场景树
            nCreature.GetTree().Root.AddChild(effectNode);

            MainFile.Logger.Info($"播放死亡特效 - 生物: {creature.Name}, 位置: {effectPosition}");
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
    private const double Duration = 2.0;

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
