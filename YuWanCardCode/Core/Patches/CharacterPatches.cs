using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

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
        if (__instance is IYuWanCharacter c && c.CustomVisualPath != null)
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

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
static class TrailPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c && c.CustomTrailPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomIconOutlineTexturePath != null)
        {
            __result = c.CustomIconOutlineTexturePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "IconTexturePath", MethodType.Getter)]
static class IconTexturePathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c && c.CustomIconTexturePath != null)
        {
            __result = c.CustomIconTexturePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "Icon", MethodType.Getter)]
static class IconPatch
{
    static bool Prefix(CharacterModel __instance, ref Control? __result)
    {
        if (__instance is IYuWanCharacter c && c.CustomIcon != null)
        {
            __result = c.CustomIcon;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "IconPath", MethodType.Getter)]
static class IconPathPatch
{
    static bool Prefix(CharacterModel __instance, ref string? __result)
    {
        if (__instance is IYuWanCharacter c && c.CustomIconPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomEnergyCounterPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomRestSiteAnimPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomMerchantAnimPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomArmPointingTexturePath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomArmRockTexturePath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomArmPaperTexturePath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomArmScissorsTexturePath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomCharacterSelectTransitionPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomCharacterSelectBg != null)
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
        if (__instance is IYuWanCharacter c && c.CustomCharacterSelectIconPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomCharacterSelectLockedIconPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomMapMarkerPath != null)
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
        if (__instance is IYuWanCharacter c && c.CustomAttackSfx != null)
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
        if (__instance is IYuWanCharacter c && c.CustomCastSfx != null)
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
        if (__instance is IYuWanCharacter c && c.CustomDeathSfx != null)
        {
            __result = c.CustomDeathSfx;
            return false;
        }
        return true;
    }
}
