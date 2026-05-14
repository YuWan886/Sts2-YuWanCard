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
    static void Postfix(NCard __instance)
    {
        if (!AncientVisualStyleHelper.UsesAncientVisualStyle(__instance.Model) || !__instance.IsNodeReady())
            return;

        var model = __instance.Model!;
        var tr = Traverse.Create(__instance);

        var portraitBorder = tr.Field<TextureRect>("_portraitBorder").Value;
        var portrait = tr.Field<TextureRect>("_portrait").Value;
        var frame = tr.Field<TextureRect>("_frame").Value;
        var ancientPortrait = tr.Field<TextureRect>("_ancientPortrait").Value;
        var ancientBorder = tr.Field<TextureRect>("_ancientBorder").Value;
        var ancientTextBg = tr.Field<TextureRect>("_ancientTextBg").Value;
        var ancientBanner = tr.Field<TextureRect>("_ancientBanner").Value;
        var banner = tr.Field<TextureRect>("_banner").Value;
        var ancientHighlight = tr.Field<CanvasItem>("_ancientHighlight").Value;
        var portraitCanvasGroup = tr.Field<CanvasGroup>("_portraitCanvasGroup").Value;

        portraitBorder.Visible = false;
        portrait.Visible = false;
        frame.Visible = false;
        banner.Visible = false;

        ancientPortrait.Visible = true;
        ancientBorder.Visible = true;
        ancientTextBg.Visible = true;
        ancientBanner.Visible = true;
        ancientHighlight.Visible = true;

        ancientPortrait.Texture = model.Portrait;
        ancientTextBg.Texture = model.AncientTextBg;
        banner.Material = null;

        if (__instance.Visibility != ModelVisibility.Visible)
        {
            var portraitBlurMaterial = PreloadManager.Cache.GetMaterial(AncientVisualStyleHelper.PortraitBlurMaterialPath);
            var canvasGroupMaskBlurMaterial = PreloadManager.Cache.GetMaterial(AncientVisualStyleHelper.CanvasGroupMaskBlurMaterialPath);
            portraitCanvasGroup.Material = canvasGroupMaskBlurMaterial;
            portrait.Material = portraitBlurMaterial;
            ancientPortrait.Material = portraitBlurMaterial;
        }
        else
        {
            var canvasGroupMaskMaterial = PreloadManager.Cache.GetMaterial(AncientVisualStyleHelper.CanvasGroupMaskMaterialPath);
            portraitCanvasGroup.Material = canvasGroupMaskMaterial;
            portrait.Material = null;
            ancientPortrait.Material = null;
        }
    }
}

[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
static class NCardAncientVisualOverlayPatch
{
    static void Postfix(NCard __instance)
    {
        if (!AncientVisualStyleHelper.UsesAncientVisualStyle(__instance.Model) || !__instance.IsNodeReady())
            return;

        var tr = Traverse.Create(__instance);
        tr.Field<TextureRect>("_frame").Value.Visible = false;
        tr.Field<TextureRect>("_ancientBorder").Value.Visible = true;
        tr.Field<CanvasItem>("_ancientHighlight").Value.Visible = true;
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
