using YuWanCard.Core.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

// --- Card custom portrait / frame ---

[HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
static class CustomCardPortraitPngPath
{
    static bool Prefix(CardModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanCardModel c && c.CustomPortraitPath != null)
        {
            __result = c.CustomPortraitPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Frame), MethodType.Getter)]
static class CustomCardFrame
{
    static bool Prefix(CardModel __instance, ref Texture2D? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanCardModel c)
        {
            __result = c.CustomFrame;
            if (__result != null) return false;
        }
        return true;
    }
}

// --- Power custom icon ---

[HarmonyPatch(typeof(PowerModel), "PackedIconPath", MethodType.Getter)]
static class CustomPowerPackedIconPath
{
    static bool Prefix(PowerModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanPowerModel p && p.CustomPackedIconPath != null)
        {
            __result = p.CustomPackedIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PowerModel), "BigIconPath", MethodType.Getter)]
static class CustomPowerBigIconPath
{
    static bool Prefix(PowerModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanPowerModel p && p.CustomBigIconPath != null)
        {
            __result = p.CustomBigIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PowerModel), "BigBetaIconPath", MethodType.Getter)]
static class CustomPowerBigBetaIconPath
{
    static bool Prefix(PowerModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanPowerModel p)
        {
            __result = p.CustomBigIconPath;
            return false;
        }
        return true;
    }
}

// --- Relic custom icon ---

[HarmonyPatch(typeof(RelicModel), "PackedIconPath", MethodType.Getter)]
static class CustomRelicPackedIconPath
{
    static bool Prefix(RelicModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanRelicModel r)
        {
            __result = r.PackedIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(RelicModel), "BigIconPath", MethodType.Getter)]
static class CustomRelicBigIconPath
{
    static bool Prefix(RelicModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanRelicModel r)
        {
            __result = r.PackedIconPath;
            return false;
        }
        return true;
    }
}

// --- Ancient custom paths ---

[HarmonyPatch(typeof(AncientEventModel), "MapIconPath", MethodType.Getter)]
static class CustomAncientMapIconPath
{
    static bool Prefix(AncientEventModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanAncientModel a && a.CustomMapIconPath != null)
        {
            __result = a.CustomMapIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(AncientEventModel), "MapIconOutlinePath", MethodType.Getter)]
static class CustomAncientMapIconOutlinePath
{
    static bool Prefix(AncientEventModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanAncientModel a && a.CustomMapIconOutlinePath != null)
        {
            __result = a.CustomMapIconOutlinePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(AncientEventModel), "RunHistoryIconOutlinePath", MethodType.Getter)]
static class CustomAncientRunHistoryIconOutlinePath
{
    static bool Prefix(AncientEventModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanAncientModel a && a.CustomRunHistoryIconOutlinePath != null)
        {
            __result = a.CustomRunHistoryIconOutlinePath;
            return false;
        }
        return true;
    }
}

// --- Encounter custom scene path ---

[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.ScenePath), MethodType.Getter)]
static class CustomEncounterScenePath
{
    static bool Prefix(EncounterModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanEncounterModel e && e.CustomScenePath != null)
        {
            __result = e.CustomScenePath;
            return false;
        }
        return true;
    }
}

// --- Enchantment custom icon ---

[HarmonyPatch(typeof(EnchantmentModel), "IconPath", MethodType.Getter)]
static class CustomEnchantmentIconPath
{
    static bool Prefix(EnchantmentModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanEnchantmentModel e)
        {
            __result = e.ResolvedCustomIconPath;
            return false;
        }
        return true;
    }
}

// --- Monster custom visuals ---

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
static class CustomMonsterVisuals
{
    static bool Prefix(MonsterModel __instance, ref MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanMonsterModel m)
        {
            __result = m.CreateCustomVisuals();
            return __result == null;
        }
        return true;
    }
}

// --- Orb custom icon / sprite ---

[HarmonyPatch(typeof(OrbModel), "IconPath", MethodType.Getter)]
static class CustomOrbIconPath
{
    static bool Prefix(OrbModel __instance, ref string __result)
    {
        if (__instance is Orbs.LittleRegentOrb orb && orb.CustomIconPath != null)
        {
            __result = orb.CustomIconPath;
            return false;
        }
        return true;
    }
}

[HarmonyPriority(Priority.First)]
[HarmonyPatch(typeof(OrbModel), nameof(OrbModel.CreateSprite))]
static class CustomOrbCreateSprite
{
    static bool Prefix(OrbModel __instance, ref Node2D __result)
    {
        if (__instance is Orbs.LittleRegentOrb orb)
        {
            var sprite = orb.CreateCustomSprite();
            if (sprite != null)
            {
                __result = sprite;
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Orbs), MethodType.Getter)]
static class CustomOrbsListPatch
{
    [HarmonyPostfix]
    static IEnumerable<OrbModel> AddCustomOrbs(IEnumerable<OrbModel> __result)
    {
        return __result.Append(ModelDb.Orb<Orbs.LittleRegentOrb>());
    }
}

// --- Potion custom image ---

[HarmonyPatch(typeof(PotionModel), "PackedImagePath", MethodType.Getter)]
static class CustomPotionPackedImagePath
{
    static bool Prefix(PotionModel __instance, ref string __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanPotionModel p && p.CustomPackedImagePath != null)
        {
            __result = p.CustomPackedImagePath;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PotionModel), "PackedOutlinePath", MethodType.Getter)]
static class CustomPotionPackedOutlinePath
{
    static bool Prefix(PotionModel __instance, ref string? __result)
    {
        if (__instance is YuWanCard.Core.Abstracts.YuWanPotionModel p && p.CustomPackedOutlinePath != null)
        {
            __result = p.CustomPackedOutlinePath;
            return false;
        }
        return true;
    }
}
