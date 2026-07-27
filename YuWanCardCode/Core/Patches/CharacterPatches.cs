using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Random;

namespace YuWanCard.Core.Patches;

// --- Character registration ---

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class ModelDbCharactersPatch
{
    public static readonly List<CharacterModel> CustomCharacters = [];

    [HarmonyPostfix]
    static IEnumerable<CharacterModel> AddCustomCharacters(IEnumerable<CharacterModel> __result)
    {
        return __result.Concat(CustomCharacters);
    }

    public static void Register(CharacterModel character)
    {
        CustomCharacters.Add(character);
    }
}

// --- Custom character path overrides ---

[HarmonyPatch(typeof(CharacterModel), "VisualsPath", MethodType.Getter)]
static class CustomCharacterVisualPath
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomVisualPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
static class CustomCharacterVisuals
{
    static bool Prefix(CharacterModel __instance, ref NCreatureVisuals? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CreateCustomVisuals();
            return __result == null;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.GenerateAnimator))]
static class GenerateAnimatorPatch
{
    static bool Prefix(CharacterModel __instance, MegaSprite controller, ref CreatureAnimator? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.SetupCustomAnimationStates(controller);
            return __result == null;
        }
        return true;
    }
}

[HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
static class FakeMerchantMissingRelaxedAnimationPatch
{
    [HarmonyPrefix]
    static bool Prefix(NCreatureVisuals visuals)
    {
        MegaSprite? spine = visuals.SpineBody;
        if (spine == null || spine.HasAnimation(CharacterModel.relaxedAnim) || !spine.HasAnimation("idle_loop"))
        {
            return true;
        }

        visuals.SpineAnimation.SetAnimation("idle_loop");
        using MegaTrackEntry? trackEntry = visuals.SpineAnimation.GetCurrentTrack();
        if (trackEntry != null)
        {
            trackEntry.SetLoop(loop: true);
            trackEntry.SetTimeScale(Rng.Chaotic.NextFloat(0.9f, 1.1f));

            float animationEnd = trackEntry.GetAnimationEnd();
            if (animationEnd > 0f)
            {
                trackEntry.SetTrackTime(
                    (animationEnd + Rng.Chaotic.NextFloat(-0.5f, 0.5f)) % animationEnd);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
static class TrailPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomTrailPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "IconOutlineTexturePath", MethodType.Getter)]
static class IconOutlineTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomIconOutlineTexturePath;
            return __result == null;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "IconTexturePath", MethodType.Getter)]
static class IconTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomIconTexturePath;
            return __result == null;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "Icon", MethodType.Getter)]
static class IconPatch
{
    static bool Prefix(CharacterModel __instance, ref Control? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomIcon;
            return __result == null;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "IconPath", MethodType.Getter)]
static class IconPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "EnergyCounterPath", MethodType.Getter)]
static class EnergyCounterPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomEnergyCounterPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "RestSiteAnimPath", MethodType.Getter)]
static class RestSiteAnimPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomRestSiteAnimPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)]
static class MerchantAnimPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomMerchantAnimPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "ArmPointingTexturePath", MethodType.Getter)]
static class ArmPointingTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomArmPointingTexturePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "ArmRockTexturePath", MethodType.Getter)]
static class ArmRockTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomArmRockTexturePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "ArmPaperTexturePath", MethodType.Getter)]
static class ArmPaperTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomArmPaperTexturePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "ArmScissorsTexturePath", MethodType.Getter)]
static class ArmScissorsTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomArmScissorsTexturePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "CharacterSelectTransitionPath", MethodType.Getter)]
static class CharacterSelectTransitionPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomCharacterSelectTransitionPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
static class CustomCharacterSelectBgPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomCharacterSelectBg;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "CharacterSelectIconPath", MethodType.Getter)]
static class CharacterSelectIconPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomCharacterSelectIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "CharacterSelectLockedIconPath", MethodType.Getter)]
static class CharacterSelectLockedIconPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomCharacterSelectLockedIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "MapMarkerPath", MethodType.Getter)]
static class MapMarkerPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomMapMarkerPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "AttackSfx", MethodType.Getter)]
static class AttackSfxPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomAttackSfx;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "CastSfx", MethodType.Getter)]
static class CastSfxPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomCastSfx;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "DeathSfx", MethodType.Getter)]
static class DeathSfxPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c)
        {
            __result = c.CustomDeathSfx;
            return false;
        }
        return true;
    }
}
