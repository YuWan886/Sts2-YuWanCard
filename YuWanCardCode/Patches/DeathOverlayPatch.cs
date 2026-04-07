using System.Linq;
using Godot;
using HarmonyLib;
using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Config;

namespace YuWanCard.Patches;

public static class DeathOverlayConstants
{
    public const string DeathOverlayScenePath = "res://YuWanCard/scenes/characters/death_overlay.tscn";
    public const float DeathAnimationDuration = 1.0f;
}

public static class DeathOverlayExtensions
{
    private static readonly SpireField<NCreature, NCreatureVisuals?> OriginalVisualsField = new(() => null);
    private static readonly SpireField<NCreature, NCreatureVisuals?> DeathVisualsField = new(() => null);

    public static NCreatureVisuals? GetOriginalVisuals(this NCreature creature) => OriginalVisualsField.Get(creature);
    public static void SetOriginalVisuals(this NCreature creature, NCreatureVisuals? visuals) => OriginalVisualsField.Set(creature, visuals);
    public static NCreatureVisuals? GetDeathVisuals(this NCreature creature) => DeathVisualsField.Get(creature);
    public static void SetDeathVisuals(this NCreature creature, NCreatureVisuals? visuals) => DeathVisualsField.Set(creature, visuals);
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public class DeathOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, bool shouldRemove)
    {
        try
        {
            if (!YuWanCardConfig.ShowDeathOverlay)
            {
                return;
            }

            if (__instance.Entity == null)
            {
                MainFile.Logger.Debug($"Death overlay skipped: Entity is null for creature");
                return;
            }

            if (__instance.Entity.IsAlive)
            {
                MainFile.Logger.Debug($"Death overlay skipped: Entity is alive for creature: {__instance.Entity.Name}");
                return;
            }

            if (__instance.Visuals == null)
            {
                MainFile.Logger.Debug($"Death overlay skipped: Visuals is null for creature: {__instance.Entity.Name}");
                return;
            }

            if (__instance.GetDeathVisuals() != null)
            {
                MainFile.Logger.Debug($"Death overlay skipped: Death visuals already exists for creature: {__instance.Entity.Name}");
                return;
            }

            if (__instance.IntentContainer != null)
            {
                __instance.IntentContainer.Modulate = new Color(
                    __instance.IntentContainer.Modulate.R,
                    __instance.IntentContainer.Modulate.G,
                    __instance.IntentContainer.Modulate.B,
                    0f
                );
                
                foreach (var intent in __instance.IntentContainer.GetChildren().OfType<NIntent>())
                {
                    intent.SetFrozen(isFrozen: true);
                }
            }

            var sceneTree = __instance.Visuals.GetTree();
            if (sceneTree == null)
            {
                MainFile.Logger.Warn($"SceneTree is null, cannot create timer for creature: {__instance.Entity?.Name}");
                return;
            }

            var delayTimer = sceneTree.CreateTimer(DeathOverlayConstants.DeathAnimationDuration);
            var creatureRef = new WeakReference<NCreature>(__instance);

            delayTimer.Timeout += () =>
            {
                try
                {
                    if (!creatureRef.TryGetTarget(out var creature) || creature == null)
                    {
                        MainFile.Logger.Debug($"Death overlay timer: Creature has been garbage collected");
                        return;
                    }

                    if (!YuWanCardConfig.ShowDeathOverlay)
                    {
                        MainFile.Logger.Debug($"Death overlay disabled during timer, skipping display for creature: {creature.Entity?.Name}");
                        return;
                    }

                    if (creature.Visuals == null)
                    {
                        MainFile.Logger.Debug($"Death overlay timer: Visuals is null for creature: {creature.Entity?.Name}");
                        return;
                    }

                    if (creature.GetDeathVisuals() != null)
                    {
                        MainFile.Logger.Debug($"Death overlay timer: Death visuals already exists for creature: {creature.Entity?.Name}");
                        return;
                    }

                    if (!ResourceLoader.Exists(DeathOverlayConstants.DeathOverlayScenePath))
                    {
                        MainFile.Logger.Warn($"Death overlay scene not found at {DeathOverlayConstants.DeathOverlayScenePath}");
                        return;
                    }

                    var deathVisuals = NodeFactory<NCreatureVisuals>.CreateFromScene(DeathOverlayConstants.DeathOverlayScenePath);
                    if (deathVisuals == null)
                    {
                        MainFile.Logger.Warn($"Failed to create death visuals from scene: {DeathOverlayConstants.DeathOverlayScenePath}");
                        return;
                    }

                    creature.SetOriginalVisuals(creature.Visuals);
                    creature.SetDeathVisuals(deathVisuals);

                    creature.Visuals.Visible = false;

                    creature.AddChild(deathVisuals);

                    // 复制原始视觉节点的大小
                    if (creature.Visuals != null)
                    {
                        // 获取原始视觉节点的缩放
                        deathVisuals.Scale = creature.Visuals.Scale;
                        
                        // 如果是敌人，水平翻转
                        if (creature.Entity != null && creature.Entity.Side == CombatSide.Enemy)
                        {
                            deathVisuals.Scale = new Vector2(-deathVisuals.Scale.X, deathVisuals.Scale.Y);
                        }
                    }

                    // 确保意图容器保持隐藏
                    if (creature.IntentContainer != null)
                    {
                        creature.IntentContainer.Modulate = new Color(
                            creature.IntentContainer.Modulate.R,
                            creature.IntentContainer.Modulate.G,
                            creature.IntentContainer.Modulate.B,
                            0f
                        );
                    }

                    MainFile.Logger.Info($"Death visuals replaced for: {creature.Entity?.Name}");
                }
                catch (System.Exception ex)
                {
                    MainFile.Logger.Error($"Error in death overlay timer: {ex.Message}");
                }
            };

            MainFile.Logger.Debug($"Death overlay scheduled for creature: {__instance.Entity?.Name}");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"Error in DeathOverlayPatch.Postfix: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
public class ReviveOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        try
        {
            if (!YuWanCardConfig.ShowDeathOverlay)
            {
                return;
            }

            if (__instance.Entity == null)
            {
                return;
            }

            var deathVisuals = __instance.GetDeathVisuals();
            if (deathVisuals != null)
            {
                deathVisuals.QueueFree();
                __instance.SetDeathVisuals(null);
                MainFile.Logger.Info($"Death visuals removed for: {__instance.Entity.Name}");
            }

            var originalVisuals = __instance.GetOriginalVisuals();
            if (originalVisuals != null)
            {
                originalVisuals.Visible = true;
                __instance.SetOriginalVisuals(null);
            }
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"Error in ReviveOverlayPatch.Postfix: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.TakeTurn))]
public class DeathTakeTurnPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Creature __instance)
    {
        // 如果生物已经死亡，不执行回合行动
        if (!__instance.IsAlive)
        {
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CombatManager))]
public class DeathPerformIntentPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("AfterAllPlayersReadyToEndTurn")]
    public static void Prefix(CombatManager __instance)
    {
        // 清理死亡敌人的意图显示
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom != null)
        {
            foreach (var creature in combatRoom.GetChildren().OfType<NCreature>())
            {
                if (creature.Entity != null && !creature.Entity.IsAlive && creature.IntentContainer != null)
                {
                    creature.IntentContainer.Modulate = new Color(
                        creature.IntentContainer.Modulate.R,
                        creature.IntentContainer.Modulate.G,
                        creature.IntentContainer.Modulate.B,
                        0f
                    );
                }
            }
        }
    }
}

[HarmonyPatch(typeof(CombatManager))]
public class DeathOverlayCombatEndCleanupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.EndCombatInternal))]
    public static void EndCombatInternalPostfix()
    {
        try
        {
            CleanupDeathOverlay();
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"Error in DeathOverlayCombatEndCleanupPatch.EndCombatInternalPostfix: {ex.Message}");
        }
    }

    public static void CleanupDeathOverlay()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null)
            {
                return;
            }

            var creatures = combatRoom.GetChildren().OfType<NCreature>().ToList();
            int cleanedCount = 0;

            foreach (var creature in creatures)
            {
                var deathVisuals = creature.GetDeathVisuals();
                if (deathVisuals != null)
                {
                    deathVisuals.QueueFree();
                    creature.SetDeathVisuals(null);
                    cleanedCount++;
                }

                var originalVisuals = creature.GetOriginalVisuals();
                if (originalVisuals != null)
                {
                    originalVisuals.Visible = true;
                    creature.SetOriginalVisuals(null);
                }
            }

            if (cleanedCount > 0)
            {
                MainFile.Logger.Info($"Death overlay cleaned up at combat end: {cleanedCount} creature(s) restored");
            }
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"Error in DeathOverlayCombatEndCleanupPatch.CleanupDeathOverlay: {ex.Message}");
        }
    }
}
