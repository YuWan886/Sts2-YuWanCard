using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Config;

namespace YuWanCard.Patches;

public static class DeathOverlayConstants
{
    public const string DeathOverlayScenePath = "res://YuWanCard/scenes/characters/death_overlay.tscn";
    public const string OverlayNodeName = "DeathOverlay";
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public class DeathOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, bool shouldRemove)
    {
        if (!YuWanCardConfig.ShowDeathOverlay)
        {
            return;
        }

        if (__instance.Visuals == null)
        {
            return;
        }

        if (__instance.Entity == null || __instance.Entity.IsAlive)
        {
            return;
        }

        if (__instance.Entity.Side != CombatSide.Player)
        {
            return;
        }

        var existingOverlay = __instance.Visuals.GetNodeOrNull<Node2D>(DeathOverlayConstants.OverlayNodeName);
        if (existingOverlay != null)
        {
            return;
        }

        if (!ResourceLoader.Exists(DeathOverlayConstants.DeathOverlayScenePath))
        {
            MainFile.Logger.Warn($"Death overlay scene not found at {DeathOverlayConstants.DeathOverlayScenePath}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(DeathOverlayConstants.DeathOverlayScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"Failed to load death overlay scene from {DeathOverlayConstants.DeathOverlayScenePath}");
            return;
        }

        var overlayNode = scene.Instantiate<Node2D>();
        if (overlayNode == null)
        {
            MainFile.Logger.Warn($"Failed to instantiate death overlay scene");
            return;
        }

        overlayNode.Name = DeathOverlayConstants.OverlayNodeName;

        // 隐藏攻击意图
        __instance.AnimHideIntent();

        var body = __instance.Visuals.GetNodeOrNull<Node2D>("%Visuals");
        if (body != null)
        {
            body.Hide();
            overlayNode.Position = body.Position;
            overlayNode.Scale = body.Scale;
        }

        __instance.Visuals.AddChild(overlayNode);

        MainFile.Logger.Info($"Death overlay scene added to creature: {__instance.Entity?.Name}");
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
public class ReviveOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        if (!YuWanCardConfig.ShowDeathOverlay)
        {
            return;
        }

        if (__instance.Visuals == null)
        {
            return;
        }

        var overlay = __instance.Visuals.GetNodeOrNull<Node2D>(DeathOverlayConstants.OverlayNodeName);
        if (overlay != null)
        {
            overlay.QueueFree();
            MainFile.Logger.Info($"Death overlay removed from creature: {__instance.Entity?.Name}");
        }

        var body = __instance.Visuals.GetNodeOrNull<Node2D>("%Visuals");
        if (body != null)
        {
            body.Show();
        }
    }
}
