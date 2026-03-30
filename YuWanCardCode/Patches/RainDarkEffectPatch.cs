using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

/// <summary>
/// 管理 RainDark 卡牌的下雨场景特效
/// </summary>
public class RainDarkEffectPatch
{
    /// <summary>
    /// 当前战斗中的下雨特效节点引用
    /// </summary>
    private static NRainVfx? _currentRainEffect;

    /// <summary>
    /// 标记是否正在播放下雨特效
    /// </summary>
    public static bool IsRaining { get; private set; }

    /// <summary>
    /// 下雨特效的剩余回合数
    /// </summary>
    public static int RainTurnsRemaining { get; private set; }

    /// <summary>
    /// 挂起的下雨效果持续时间（用于战斗场景加载后重试）
    /// </summary>
    private static int? _pendingRainDuration;

    /// <summary>
    /// 添加下雨特效到战斗场景
    /// </summary>
    /// <param name="duration">持续回合数</param>
    public static void AddRainEffect(int duration = 3)
    {
        if (IsRaining)
        {
            RainTurnsRemaining = Math.Max(RainTurnsRemaining, duration);
            return;
        }

        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null)
            {
                MainFile.Logger.Warn("RainDarkEffectPatch: CombatRoom not found, rain effect delayed");
                _pendingRainDuration = duration;
                return;
            }

            var vfxContainer = combatRoom.CombatVfxContainer;
            if (vfxContainer == null)
            {
                MainFile.Logger.Error("RainDarkEffectPatch: CombatVfxContainer not found, rain effect delayed");
                _pendingRainDuration = duration;
                return;
            }

            // 使用游戏官方的 NRainVfx 创建下雨特效
            var rainNode = NRainVfx.Create();
            if (rainNode == null)
            {
                MainFile.Logger.Error("RainDarkEffectPatch: Failed to create NRainVfx, rain effect delayed");
                _pendingRainDuration = duration;
                return;
            }

            // 添加到战斗场景的 VFX 容器
            vfxContainer.AddChild(rainNode);
            _currentRainEffect = rainNode;
            IsRaining = true;
            RainTurnsRemaining = duration;
            _pendingRainDuration = null;
            MainFile.Logger.Info($"RainDarkEffectPatch: Rain effect added for {duration} turns");

        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"RainDarkEffectPatch: Failed to add rain effect - {ex.Message}");
            MainFile.Logger.Error($"RainDarkEffectPatch: Stack trace - {ex.StackTrace}");
            _pendingRainDuration = duration;
        }
    }

    /// <summary>
    /// 尝试应用挂起的下雨特效
    /// </summary>
    public static void TryApplyPendingRainEffect()
    {
        if (_pendingRainDuration.HasValue && !IsRaining)
        {
            MainFile.Logger.Info($"RainDarkEffectPatch: Applying pending rain effect for {_pendingRainDuration.Value} turns");
            AddRainEffect(_pendingRainDuration.Value);
        }
    }

    /// <summary>
    /// 移除下雨特效
    /// </summary>
    public static void RemoveRainEffect()
    {
        try
        {
            if (_currentRainEffect != null)
            {
                _currentRainEffect.QueueFree();
                _currentRainEffect = null;
            }

            IsRaining = false;
            RainTurnsRemaining = 0;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"RainDarkEffectPatch: Failed to remove rain effect - {ex.Message}");
        }
    }

    /// <summary>
    /// 减少下雨特效剩余回合数
    /// </summary>
    public static void DecrementRainTurns()
    {
        if (IsRaining)
        {
            RainTurnsRemaining--;
            if (RainTurnsRemaining <= 0)
            {
                RemoveRainEffect();
            }
        }
    }

    /// <summary>
    /// 清理战斗结束时的下雨特效
    /// </summary>
    public static void CleanupAfterCombat()
    {
        if (IsRaining)
        {
            RemoveRainEffect();
        }
    }
} 
