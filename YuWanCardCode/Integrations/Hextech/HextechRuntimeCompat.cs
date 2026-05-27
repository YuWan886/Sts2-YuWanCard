using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Relics;
using YuWanCard.Hextech.Relics;

namespace YuWanCard.Hextech;

public static class HextechRuntimeCompat
{
    private const string HextechModId = "HextechRunes";
    private const string HextechCatalogTypeName = "HextechRunes.HextechCatalog";
    private const string HextechRuneGrantHelperTypeName = "HextechRunes.HextechRuneGrantHelper";
    private const string HextechRuneSelectionCoordinatorTypeName = "HextechRunes.HextechRuneSelectionCoordinator";
    private const string HextechMayhemModifierTypeName = "HextechRunes.HextechMayhemModifier";
    private const string HextechTelemetryTypeName = "HextechRunes.HextechTelemetry";
    private const string UnlockStateTypeName = "MegaCrit.Sts2.Core.Unlocks.UnlockState";
    private const string SaveManagerTypeName = "MegaCrit.Sts2.Core.Saves.SaveManager";
    private const string EnergyIconHelperTypeName = "MegaCrit.Sts2.Core.Helpers.EnergyIconHelper";
    private static bool _installed;
    private static readonly AsyncLocal<int> OwnedRuneRecognitionScopeDepth = new();

    public static void TryInstall(Harmony harmony)
    {
        if (_installed)
        {
            return;
        }

        if (!IsHextechLoaded(out Assembly? hextechAssembly))
        {
            return;
        }

        _installed = true;
        MainFile.Logger.Info("HextechRuntimeCompat: HextechRunes detected, applying Pig rune runtime integration");
        PatchHextechCatalog(harmony, hextechAssembly!);
        PatchScopedRuntimeRecognition(harmony, hextechAssembly!);
        PatchCompendiumDisplayCompat(harmony);
    }

    public static void TryInstallIfAvailable()
    {
        TryInstall(new Harmony(MainFile.ModId));
    }

    private static bool IsHextechLoaded(out Assembly? assembly)
    {
        assembly = null;
        foreach (var mod in ModManager.GetLoadedMods())
        {
            if (mod.manifest?.id == HextechModId && mod.assembly != null)
            {
                assembly = mod.assembly;
                return true;
            }
        }

        return false;
    }

    private static void PatchHextechCatalog(Harmony harmony, Assembly hextechAssembly)
    {
        Type? catalogType = hextechAssembly.GetType(HextechCatalogTypeName);
        if (catalogType == null)
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: HextechCatalog type not found");
            return;
        }

        PatchMethod(harmony, catalogType, "GetAllSelectableRuneTypes", nameof(GetAllSelectableRuneTypesPostfix));
        PatchMethod(harmony, catalogType, "GetGenericSelectableRuneTypes", nameof(GetGenericSelectableRuneTypesPostfix));
        PatchMethod(harmony, catalogType, "GetPlayerRuneTypesForRarity", nameof(GetPlayerRuneTypesForRarityPostfix));
        PatchMethod(harmony, catalogType, "GetCharacterRuneGroups", nameof(GetCharacterRuneGroupsPostfix));
        PatchMethod(harmony, catalogType, "IsPlayerRuneTypeSelectable", nameof(IsPlayerRuneTypeSelectablePostfix));
        PatchMethod(harmony, catalogType, "GetPlayerRunePoolKey", nameof(GetPlayerRunePoolKeyPostfix));
        PatchMethod(harmony, catalogType, "IsAvailableForPlayer", nameof(IsAvailableForPlayerPostfix));
        PatchMethod(harmony, catalogType, "IsPlayerRuneAllowedInAct", nameof(IsPlayerRuneAllowedInActPostfix));
        PatchMethod(harmony, catalogType, "IsHextechRelic", nameof(IsHextechRelicScopedPostfix));
        PatchMethod(harmony, catalogType, "TryGetPlayerRuneRarity", nameof(TryGetPlayerRuneRarityScopedPostfix));
        PatchMethod(harmony, catalogType, "GetMutuallyExclusivePlayerRuneIds", nameof(GetMutuallyExclusivePlayerRuneIdsPostfix));
    }

    private static void PatchMethod(Harmony harmony, Type type, string methodName, string postfixName)
    {
        MethodInfo? original = AccessTools.Method(type, methodName);
        MethodInfo? postfix = AccessTools.Method(typeof(HextechRuntimeCompat), postfixName);
        if (original == null || postfix == null)
        {
            MainFile.Logger.Warn($"HextechRuntimeCompat: skipped patch {type.Name}.{methodName}");
            return;
        }

        harmony.Patch(original, postfix: new HarmonyMethod(postfix));
    }

    private static void PatchScopedRuntimeRecognition(Harmony harmony, Assembly hextechAssembly)
    {
        PatchScopedMethods(
            harmony,
            hextechAssembly.GetType(HextechRuneGrantHelperTypeName),
            "BuildObtainableRunePool",
            "ReplaceOwnedHextechRunesWithRandomRunes");
        PatchScopedMethods(
            harmony,
            hextechAssembly.GetType(HextechRuneSelectionCoordinatorTypeName),
            "BuildSelectableRunePool");
        PatchScopedMethods(
            harmony,
            hextechAssembly.GetType(HextechMayhemModifierTypeName),
            "GetHighestActResolvedByPlayerRuneCounts",
            "TryInferRarityForActFromPlayerRelics",
            "DescribePlayerHexCounts",
            "GetMinimumPlayerHexCount");
        PatchScopedMethods(
            harmony,
            hextechAssembly.GetType(HextechTelemetryTypeName),
            "CreateRunTelemetry");
    }

    private static void PatchScopedMethods(Harmony harmony, Type? type, params string[] methodNames)
    {
        if (type == null)
        {
            return;
        }

        MethodInfo? prefix = AccessTools.Method(typeof(HextechRuntimeCompat), nameof(BeginOwnedRuneRecognitionScope));
        MethodInfo? postfix = AccessTools.Method(typeof(HextechRuntimeCompat), nameof(EndOwnedRuneRecognitionScope));
        foreach (string methodName in methodNames)
        {
            MethodInfo? original = AccessTools.Method(type, methodName);
            if (original == null || prefix == null || postfix == null)
            {
                MainFile.Logger.Warn($"HextechRuntimeCompat: skipped scoped patch {type.Name}.{methodName}");
                continue;
            }

            harmony.Patch(original, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
        }
    }

    private static void PatchCompendiumDisplayCompat(Harmony harmony)
    {
        Type? unlockStateType = AccessTools.TypeByName(UnlockStateTypeName);
        Type? saveManagerType = AccessTools.TypeByName(SaveManagerTypeName);
        Type? energyIconHelperType = AccessTools.TypeByName(EnergyIconHelperTypeName);

        MethodInfo? unlockStateRelicsGetter = AccessTools.PropertyGetter(unlockStateType, "Relics");
        MethodInfo? isRelicSeenMethod = AccessTools.Method(saveManagerType, "IsRelicSeen");
        MethodInfo? energyPrefixMethod = AccessTools.Method(energyIconHelperType, "GetPrefix");

        MethodInfo? unlockStateRelicsPostfix = AccessTools.Method(typeof(HextechRuntimeCompat), nameof(UnlockStateRelicsPostfix));
        MethodInfo? isRelicSeenPostfix = AccessTools.Method(typeof(HextechRuntimeCompat), nameof(IsRelicSeenPostfix));
        MethodInfo? energyPrefixPostfix = AccessTools.Method(typeof(HextechRuntimeCompat), nameof(EnergyIconHelperGetPrefixPostfix));

        if (unlockStateRelicsGetter != null && unlockStateRelicsPostfix != null)
        {
            harmony.Patch(unlockStateRelicsGetter, postfix: new HarmonyMethod(unlockStateRelicsPostfix));
        }
        else
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: skipped patch UnlockState.Relics");
        }

        if (isRelicSeenMethod != null && isRelicSeenPostfix != null)
        {
            harmony.Patch(isRelicSeenMethod, postfix: new HarmonyMethod(isRelicSeenPostfix));
        }
        else
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: skipped patch SaveManager.IsRelicSeen");
        }

        if (energyPrefixMethod != null && energyPrefixPostfix != null)
        {
            harmony.Patch(energyPrefixMethod, postfix: new HarmonyMethod(energyPrefixPostfix));
        }
        else
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: skipped patch EnergyIconHelper.GetPrefix");
        }
    }

    public static void BeginOwnedRuneRecognitionScope(out bool __state)
    {
        OwnedRuneRecognitionScopeDepth.Value++;
        __state = true;
    }

    public static void EndOwnedRuneRecognitionScope(bool __state)
    {
        if (!__state)
        {
            return;
        }

        OwnedRuneRecognitionScopeDepth.Value = Math.Max(0, OwnedRuneRecognitionScopeDepth.Value - 1);
    }

    private static bool IsOwnedRuneRecognitionScopeActive()
    {
        return OwnedRuneRecognitionScopeDepth.Value > 0;
    }

    private static IEnumerable<Type> GetPigRunesForHextechRarity(object rarity)
    {
        string name = rarity.ToString() ?? string.Empty;
        return name switch
        {
            "Silver" => HextechPigRuneRegistry.GetRunesByRarity(HextechRuneRarity.Silver),
            "Gold" => HextechPigRuneRegistry.GetRunesByRarity(HextechRuneRarity.Gold),
            "Prismatic" => HextechPigRuneRegistry.GetRunesByRarity(HextechRuneRarity.Prismatic),
            _ => Array.Empty<Type>()
        };
    }

    public static void GetAllSelectableRuneTypesPostfix(ref IReadOnlyList<Type> __result)
    {
        __result = __result.Concat(HextechPigRuneRegistry.GetAllRunes()).Distinct().ToArray();
    }

    public static void GetGenericSelectableRuneTypesPostfix(ref IReadOnlyList<Type> __result)
    {
        __result = __result
            .Where(type => !HextechPigRuneRegistry.GetAllPigRunes().Contains(type))
            .Distinct()
            .ToArray();
    }

    public static void GetPlayerRuneTypesForRarityPostfix(object rarity, ref IReadOnlyList<Type> __result)
    {
        __result = __result.Concat(GetPigRunesForHextechRarity(rarity)).Distinct().ToArray();
    }

    public static void GetCharacterRuneGroupsPostfix(ref object __result)
    {
        IEnumerable<object> existingGroups = (__result as System.Collections.IEnumerable)?.Cast<object>()
            ?? Array.Empty<object>();
        Type? groupType = existingGroups.FirstOrDefault()?.GetType()
            ?? AccessTools.TypeByName("HextechRunes.HextechCatalog+RuneSeriesGroup");
        if (groupType == null)
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: skipped pig character rune group injection because RuneSeriesGroup type was unavailable");
            return;
        }

        ConstructorInfo? ctor = groupType.GetConstructor([typeof(string), typeof(IReadOnlyList<RelicModel>)]);
        if (ctor == null)
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: skipped pig character rune group injection because RuneSeriesGroup constructor was unavailable");
            return;
        }

        IReadOnlyList<RelicModel> pigRelics = HextechPigRuneRegistry.GetAllPigRunes()
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
        object pigGroup = ctor.Invoke(["CHARACTER.PIG", pigRelics]);
        object[] groups = existingGroups
            .Where(group => !HasPigCharacterGroup(group))
            .Concat([pigGroup])
            .ToArray();
        Array typedGroups = Array.CreateInstance(groupType, groups.Length);
        for (int i = 0; i < groups.Length; i++)
        {
            typedGroups.SetValue(groups[i], i);
        }

        __result = typedGroups;
    }

    public static void IsPlayerRuneTypeSelectablePostfix(Type runeType, ref bool __result)
    {
        if (HextechPigRuneRegistry.GetAllRunes().Contains(runeType))
        {
            __result = true;
        }
    }

    public static void GetPlayerRunePoolKeyPostfix(RelicModel relic, ref string __result)
    {
        if (HextechPigRuneRegistry.IsPigRune(relic))
        {
            __result = HextechRunePoolKey.Pig;
        }
        else if (HextechPigRuneRegistry.IsSharedRune(relic))
        {
            __result = HextechRunePoolKey.Generic;
        }
    }

    public static void IsAvailableForPlayerPostfix(RelicModel relic, MegaCrit.Sts2.Core.Entities.Players.Player player, ref bool __result)
    {
        if (HextechPigRuneRegistry.IsPigRune(relic))
        {
            __result = HextechPigRuneRegistry.IsAvailableForPlayer(relic, player);
            if (__result && relic is HextechPigRuneBase pigRune)
            {
                __result = pigRune.IsAvailableForPlayer(player);
            }
        }
        else if (HextechPigRuneRegistry.IsSharedRune(relic))
        {
            __result = true;
        }
        else if (HextechForgeRegistry.IsPigForge(relic))
        {
            __result = HextechForgeRegistry.IsAvailableForPlayer(relic, player);
            if (__result && relic is HextechPigForgeBase pigForge)
            {
                __result = pigForge.IsAvailableForPlayer(player);
            }
        }
    }

    public static void IsPlayerRuneAllowedInActPostfix(Type runeType, int actIndex, ref bool __result)
    {
        if (HextechPigRuneRegistry.GetAllRunes().Contains(runeType))
        {
            __result = HextechPigRuneRegistry.IsAllowedInAct(runeType, actIndex);
        }
    }

    public static void IsHextechRelicScopedPostfix(RelicModel? relic, ref bool __result)
    {
        if (!__result && IsOwnedRuneRecognitionScopeActive() && HextechPigRuneRegistry.IsPigOrSharedRune(relic))
        {
            __result = true;
        }
    }

    public static void TryGetPlayerRuneRarityScopedPostfix(RelicModel? relic, out object __state, ref bool __result, ref object rarity)
    {
        __state = rarity;
        if (__result || !IsOwnedRuneRecognitionScopeActive() || !HextechPigRuneRegistry.TryGetRarity(relic, out HextechRuneRarity pigRarity))
        {
            return;
        }

        if (__state == null)
        {
            return;
        }

        Type rarityType = __state.GetType();
        if (Enum.TryParse(rarityType, pigRarity.ToString(), out object? parsed))
        {
            rarity = parsed;
            __result = true;
        }
    }

    public static void GetMutuallyExclusivePlayerRuneIdsPostfix(IEnumerable<ModelId> ownedIds, ref IReadOnlySet<ModelId> __result)
    {
        HashSet<ModelId> union = __result.ToHashSet();
        union.UnionWith(HextechPigRuneRegistry.GetMutuallyExclusiveRuneIds(ownedIds));
        __result = union;
    }

    public static void UnlockStateRelicsPostfix(ref IEnumerable<RelicModel> __result)
    {
        __result = (__result ?? Array.Empty<RelicModel>())
            .Concat(HextechPigRuneRegistry.GetAllRunes().Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type))))
            .Distinct()
            .ToArray();
    }

    public static void IsRelicSeenPostfix(RelicModel relic, ref bool __result)
    {
        if (!__result && HextechPigRuneRegistry.IsPigOrSharedRune(relic))
        {
            __result = true;
        }
    }

    public static void EnergyIconHelperGetPrefixPostfix(AbstractModel model, ref string __result)
    {
        if (model is RelicModel relic && HextechPigRuneRegistry.IsPigOrSharedRune(relic))
        {
            __result = ModelDb.CardPool<Characters.PigCardPool>().EnergyColorName;
        }
    }

    private static bool HasPigCharacterGroup(object group)
    {
        PropertyInfo? keyProperty = group.GetType().GetProperty("LocalizationKey");
        return string.Equals(keyProperty?.GetValue(group) as string, "CHARACTER.PIG", StringComparison.Ordinal);
    }

}
