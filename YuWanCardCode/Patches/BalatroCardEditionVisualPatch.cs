using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YuWanCard.Balatro;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCard))]
public static class BalatroCardEditionVisualPatch
{
    private const string OverlayName = "YuWanBalatroEditionOverlay";
    private const string ShaderPath = "res://YuWanCard/shaders/ui/balatro_card_edition_border.gdshader";

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCard.Reload))]
    public static void OnReload(NCard __instance)
    {
        ApplyEditionVisuals(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCard.OnFreedToPool))]
    public static void OnFreedToPool(NCard __instance)
    {
        RemoveEditionVisuals(__instance);
    }

    private static void ApplyEditionVisuals(NCard card)
    {
        RemoveEditionVisuals(card);
        if (!card.IsNodeReady() || card.Body == null || card.Model == null)
        {
            return;
        }

        BalatroCardEdition edition = BalatroCardEditionHelper.GetEdition(card.Model);
        if (edition == BalatroCardEdition.None)
        {
            return;
        }

        Shader shader = GD.Load<Shader>(ShaderPath);
        if (shader == null)
        {
            return;
        }

        ColorRect overlay = new()
        {
            Name = OverlayName,
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 8
        };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        ShaderMaterial material = new()
        {
            Shader = shader
        };
        ApplyEditionMaterial(material, edition);
        overlay.Material = material;
        card.Body.AddChild(overlay);

    }

    private static void RemoveEditionVisuals(NCard card)
    {
        Control? body = card.Body;
        if (body == null || !GodotObject.IsInstanceValid(body))
        {
            return;
        }

        body.GetNodeOrNull<CanvasItem>(OverlayName)?.QueueFree();
    }

    private static void ApplyEditionMaterial(ShaderMaterial material, BalatroCardEdition edition)
    {
        material.SetShaderParameter("border_width", 0.06f);
        material.SetShaderParameter("pulse_speed", 1.6f);

        switch (edition)
        {
            case BalatroCardEdition.Foil:
                material.SetShaderParameter("mode", 0);
                material.SetShaderParameter("color_a", new Color(0.95f, 0.95f, 1f, 1f));
                material.SetShaderParameter("color_b", new Color(0.72f, 0.76f, 0.84f, 1f));
                break;
            case BalatroCardEdition.Holographic:
                material.SetShaderParameter("mode", 1);
                material.SetShaderParameter("color_a", new Color(0.32f, 0.72f, 1f, 1f));
                material.SetShaderParameter("color_b", new Color(0.71f, 0.38f, 1f, 1f));
                break;
            case BalatroCardEdition.Polychrome:
                material.SetShaderParameter("mode", 2);
                material.SetShaderParameter("color_a", new Color(1f, 0.28f, 0.39f, 1f));
                material.SetShaderParameter("color_b", new Color(0.19f, 0.89f, 1f, 1f));
                break;
            case BalatroCardEdition.Negative:
                material.SetShaderParameter("mode", 3);
                material.SetShaderParameter("color_a", new Color(0.64f, 0.26f, 1f, 1f));
                material.SetShaderParameter("color_b", new Color(0.2f, 0.08f, 0.39f, 1f));
                break;
        }
    }

}
