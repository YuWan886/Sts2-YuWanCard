using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Config;

namespace YuWanCard.Patches;

/// <summary>
/// 死亡叠加层常量定义
/// </summary>
public static class DeathOverlayConstants
{
    public const string DeathOverlayScenePath = "res://YuWanCard/scenes/characters/death_overlay.tscn";
    public const string OverlayNodeName = "DeathOverlay";
    public const string TimerNodeName = "DeathOverlayTimer";
    
    // 死亡动画持续时间（秒）
    public const float DeathAnimationDuration = 1.0f;
    
    // 叠加层显示时间（秒）
    public const float OverlayDisplayDuration = 5.0f;
}

/// <summary>
/// 死亡叠加层补丁
/// 在角色死亡后，在其尸体位置显示死亡叠加效果
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public class DeathOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, bool shouldRemove)
    {
        // 检查配置是否启用死亡叠加层
        if (!YuWanCardConfig.ShowDeathOverlay)
        {
            return;
        }

        // 安全检查：确保视觉节点存在
        if (__instance.Visuals == null)
        {
            MainFile.Logger.Debug($"Death overlay skipped: Visuals is null for creature");
            return;
        }

        // 安全检查：确保实体存在且已死亡
        if (__instance.Entity == null || __instance.Entity.IsAlive)
        {
            MainFile.Logger.Debug($"Death overlay skipped: Entity is null or alive");
            return;
        }

        // 检查是否已经存在叠加层，避免重复添加
        var existingOverlay = __instance.Visuals.GetNodeOrNull<Node2D>(DeathOverlayConstants.OverlayNodeName);
        if (existingOverlay != null)
        {
            MainFile.Logger.Debug($"Death overlay skipped: Overlay already exists for creature: {__instance.Entity?.Name}");
            return;
        }

        // 检查场景文件是否存在
        if (!ResourceLoader.Exists(DeathOverlayConstants.DeathOverlayScenePath))
        {
            MainFile.Logger.Warn($"Death overlay scene not found at {DeathOverlayConstants.DeathOverlayScenePath}");
            return;
        }

        // 加载场景
        var scene = ResourceLoader.Load<PackedScene>(DeathOverlayConstants.DeathOverlayScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"Failed to load death overlay scene from {DeathOverlayConstants.DeathOverlayScenePath}");
            return;
        }

        // 实例化叠加层节点
        var overlayNode = scene.Instantiate<Node2D>();
        if (overlayNode == null)
        {
            MainFile.Logger.Warn($"Failed to instantiate death overlay scene");
            return;
        }

        // 获取身体节点
        var body = __instance.Visuals.GetNodeOrNull<Node2D>("%Visuals");
        if (body == null)
        {
            MainFile.Logger.Warn($"Body node not found for creature: {__instance.Entity?.Name}");
            overlayNode.QueueFree();
            return;
        }

        // 配置叠加层节点
        overlayNode.Name = DeathOverlayConstants.OverlayNodeName;
        overlayNode.Visible = false; // 初始隐藏，等待死亡动画播放完毕

        // 立即隐藏攻击意图
        __instance.AnimHideIntent();

        // 将叠加层添加到场景树（但保持隐藏）
        __instance.Visuals.AddChild(overlayNode);

        // 创建延迟计时器，等待死亡动画播放完毕
        // 使用 SceneTreeTimer 而不是 Timer 节点，避免节点管理的复杂性
        var sceneTree = __instance.Visuals.GetTree();
        if (sceneTree == null)
        {
            MainFile.Logger.Warn($"SceneTree is null, cannot create timer for creature: {__instance.Entity?.Name}");
            overlayNode.QueueFree();
            return;
        }

        // 创建死亡动画延迟计时器
        var delayTimer = sceneTree.CreateTimer(DeathOverlayConstants.DeathAnimationDuration);
        
        // 使用弱引用来避免内存泄漏
        var creatureRef = new WeakReference<NCreature>(__instance);
        var overlayRef = new WeakReference<Node2D>(overlayNode);
        var bodyRef = new WeakReference<Node2D>(body);

        delayTimer.Timeout += () =>
        {
            // 安全检查：确保对象仍然存在
            if (!creatureRef.TryGetTarget(out var creature) || creature == null)
            {
                MainFile.Logger.Debug($"Death overlay timer: Creature has been garbage collected");
                return;
            }

            if (!overlayRef.TryGetTarget(out var overlay) || overlay == null)
            {
                MainFile.Logger.Debug($"Death overlay timer: Overlay has been garbage collected");
                return;
            }

            if (!bodyRef.TryGetTarget(out var bodyNode) || bodyNode == null)
            {
                MainFile.Logger.Debug($"Death overlay timer: Body has been garbage collected");
                return;
            }

            // 安全检查：确保视觉节点仍然存在
            if (creature.Visuals == null)
            {
                MainFile.Logger.Debug($"Death overlay timer: Visuals is null");
                return;
            }

            // 再次隐藏攻击意图（确保敌人也能正确隐藏）
            creature.AnimHideIntent();

            // 隐藏身体节点
            bodyNode.Hide();

            // 设置叠加层位置和缩放，与身体节点对齐
            overlay.Position = bodyNode.Position;
            overlay.Scale = bodyNode.Scale;

            // 显示叠加层
            overlay.Visible = true;

            MainFile.Logger.Info($"Death overlay shown for creature: {creature.Entity?.Name}");

            // 创建移除计时器，在指定时间后自动移除叠加层
            var removeTimer = sceneTree.CreateTimer(DeathOverlayConstants.OverlayDisplayDuration);
            removeTimer.Timeout += () =>
            {
                // 安全检查：确保叠加层仍然存在
                if (!overlayRef.TryGetTarget(out var overlayToRemove) || overlayToRemove == null)
                {
                    MainFile.Logger.Debug($"Death overlay remove timer: Overlay has been garbage collected");
                    return;
                }

                // 安全检查：确保叠加层仍然在场景树中
                if (!overlayToRemove.IsInsideTree())
                {
                    MainFile.Logger.Debug($"Death overlay remove timer: Overlay is not in scene tree");
                    return;
                }

                // 移除叠加层
                overlayToRemove.QueueFree();
                MainFile.Logger.Info($"Death overlay removed after {DeathOverlayConstants.OverlayDisplayDuration} seconds for creature: {creature.Entity?.Name}");
            };
        };

        MainFile.Logger.Debug($"Death overlay scheduled for creature: {__instance.Entity?.Name}");
    }
}

/// <summary>
/// 复活叠加层补丁
/// 在角色复活时，移除死亡叠加层并恢复身体显示
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
public class ReviveOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        // 检查配置是否启用死亡叠加层
        if (!YuWanCardConfig.ShowDeathOverlay)
        {
            return;
        }

        // 安全检查：确保视觉节点存在
        if (__instance.Visuals == null)
        {
            return;
        }

        // 移除死亡叠加层
        var overlay = __instance.Visuals.GetNodeOrNull<Node2D>(DeathOverlayConstants.OverlayNodeName);
        if (overlay != null)
        {
            overlay.QueueFree();
            MainFile.Logger.Info($"Death overlay removed from creature: {__instance.Entity?.Name}");
        }

        // 显示身体节点
        var body = __instance.Visuals.GetNodeOrNull<Node2D>("%Visuals");
        if (body != null)
        {
            body.Show();
        }
    }
}
