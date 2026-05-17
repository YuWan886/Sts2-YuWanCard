using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

internal static class AncientVisualStyleHelper
{
    public const string PortraitBlurMaterialPath = "res://scenes/cards/card_portrait_blur_material.tres";
    public const string CanvasGroupMaskMaterialPath = "res://scenes/cards/card_canvas_group_mask_material.tres";
    public const string CanvasGroupMaskBlurMaterialPath = "res://scenes/cards/card_canvas_group_mask_blur_material.tres";

    public static bool UsesAncientVisualStyle(CardModel? model) =>
        model is YuWanCardModel { UseAncientVisualStyle: true };
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.AncientTextBg), MethodType.Getter)]
static class CustomAncientTextBgPatch
{
    static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is YuWanCardModel { CustomAncientTextBg: not null } c)
        {
            __result = c.CustomAncientTextBg;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.BannerTexture), MethodType.Getter)]
static class CustomAncientBannerTexturePatch
{
    static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is YuWanCardModel { CustomBannerTexture: not null } c)
        {
            __result = c.CustomBannerTexture;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.BannerMaterial), MethodType.Getter)]
static class CustomAncientBannerMaterialPatch
{
    static bool Prefix(CardModel __instance, ref Material __result)
    {
        if (__instance is YuWanCardModel { CustomBannerMaterial: not null } c)
        {
            __result = c.CustomBannerMaterial;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(NCard), "GetTitleLabelOutlineColor")]
static class NCardAncientTitleOutlinePatch
{
    static void Postfix(NCard __instance, ref Color __result)
    {
        if (AncientVisualStyleHelper.UsesAncientVisualStyle(__instance.Model))
            __result = StsColors.cardTitleOutlineCommon;
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
static class NCardAncientVisualReloadPatch
{
    static bool Prefix(NCard __instance)
    {
        if (!AncientVisualStyleHelper.UsesAncientVisualStyle(__instance.Model))
            return true;

        if (!__instance.IsNodeReady() || __instance.Model == null)
            return false;

        if (OS.HasFeature("editor"))
            __instance.Name = $"{typeof(NCard)}-{__instance.Model.Id}";

        var tr = Traverse.Create(__instance);
        var model = __instance.Model;

        tr.Field<TextureRect>("_energyIcon").Value.Texture = model.EnergyIcon;
        tr.Method("UpdateTypePlaque").GetValue();

        var portraitBorder = tr.Field<TextureRect>("_portraitBorder").Value;
        var portrait = tr.Field<TextureRect>("_portrait").Value;
        var frame = tr.Field<TextureRect>("_frame").Value;
        var ancientPortrait = tr.Field<TextureRect>("_ancientPortrait").Value;
        var ancientBorder = tr.Field<TextureRect>("_ancientBorder").Value;
        var ancientTextBg = tr.Field<TextureRect>("_ancientTextBg").Value;
        var ancientBanner = tr.Field<Control>("_ancientBanner").Value;
        var banner = tr.Field<TextureRect>("_banner").Value;
        var lockIcon = tr.Field<TextureRect>("_lock").Value;
        var portraitCanvasGroup = tr.Field<CanvasGroup>("_portraitCanvasGroup").Value;
        var useAncientLayout = true;

        portraitBorder.Visible = !useAncientLayout;
        portrait.Visible = !useAncientLayout;
        frame.Visible = !useAncientLayout;
        ancientPortrait.Visible = useAncientLayout;
        ancientBorder.Visible = useAncientLayout;
        ancientTextBg.Visible = useAncientLayout;
        ancientBanner.Visible = useAncientLayout;
        banner.Visible = !useAncientLayout;
        lockIcon.Visible = __instance.Visibility == ModelVisibility.Locked;

        var portraitTexture = model.Portrait;

        if (__instance.Visibility != ModelVisibility.Visible)
        {
            var portraitBlurMaterial = PreloadManager.Cache.GetMaterial(AncientVisualStyleHelper.PortraitBlurMaterialPath);
            portraitCanvasGroup.Material = PreloadManager.Cache.GetMaterial(AncientVisualStyleHelper.CanvasGroupMaskBlurMaterialPath);
            portrait.Material = portraitBlurMaterial;
            ancientPortrait.Material = portraitBlurMaterial;
        }
        else
        {
            portraitCanvasGroup.Material = PreloadManager.Cache.GetMaterial(AncientVisualStyleHelper.CanvasGroupMaskMaterialPath);
            portrait.Material = null;
            ancientPortrait.Material = null;
        }

        ancientTextBg.Texture = model.AncientTextBg;
        ancientPortrait.Texture = portraitTexture;
        portraitBorder.Material = null;
        portraitBorder.Texture = null;
        banner.Texture = null;
        banner.Material = null;
        frame.Material = model.FrameMaterial;

        tr.Method("ReloadOverlay").GetValue();
        return false;
    }
}

[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
static class NCardAncientVisualOverlayPatch
{
    static bool Prefix(NCard __instance)
    {
        if (!AncientVisualStyleHelper.UsesAncientVisualStyle(__instance.Model))
            return true;

        if (!__instance.IsNodeReady() || __instance.Model == null)
            return false;

        var tr = Traverse.Create(__instance);
        var cardOverlay = tr.Field<Control?>("_cardOverlay").Value;
        var overlayContainer = tr.Field<Node>("_overlayContainer").Value;

        if (cardOverlay != null)
        {
            overlayContainer.RemoveChild(cardOverlay);
            cardOverlay.QueueFree();
            tr.Field<Control?>("_cardOverlay").Value = null;
        }

        tr.Field<TextureRect>("_frame").Value.Visible = false;
        tr.Field<TextureRect>("_ancientBorder").Value.Visible = true;
        tr.Field<CanvasItem>("_ancientHighlight").Value.Visible = true;

        Control? newOverlay = null;
        var model = __instance.Model;
        if (model.Affliction is { HasOverlay: true })
        {
            newOverlay = model.Affliction.CreateOverlay();
        }
        else if (model.HasBuiltInOverlay)
        {
            newOverlay = model.CreateOverlay();
        }

        if (newOverlay != null)
        {
            overlayContainer.AddChild(newOverlay);
            tr.Field<Control?>("_cardOverlay").Value = newOverlay;
        }

        return false;
    }
}

[HarmonyPatch(typeof(NCard), nameof(NCard.ActivateRewardScreenGlow))]
static class NCardAncientRewardGlowPatch
{
    static bool Prefix(NCard __instance)
    {
        return !AncientVisualStyleHelper.UsesAncientVisualStyle(__instance.Model);
    }
}
