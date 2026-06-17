using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Interop;
using YuWanCard.Relics;
using YuWanCard.Hextech.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Hextech;

public static class HextechRuntimeCompat
{
    private const string HextechModId = "HextechRunes";
    private const string HextechCatalogTypeName = "HextechRunes.HextechCatalog";
    private const string HextechRuneGrantHelperTypeName = "HextechRunes.HextechRuneGrantHelper";
    private const string HextechForgeGrantHelperTypeName = "HextechRunes.HextechForgeGrantHelper";
    private const string RandomForgeShopRelicTypeName = "HextechRunes.RandomForgeShopRelic";
    private const string HextechRuneSelectionCoordinatorTypeName = "HextechRunes.HextechRuneSelectionCoordinator";
    private const string HextechMayhemActRecoveryTypeName = "HextechRunes.HextechMayhemActRecovery";
    private const string HextechTelemetryTypeName = "HextechRunes.HextechTelemetry";
    private const string UnlockStateTypeName = "MegaCrit.Sts2.Core.Unlocks.UnlockState";
    private const string SaveManagerTypeName = "MegaCrit.Sts2.Core.Saves.SaveManager";
    private const string EnergyIconHelperTypeName = "MegaCrit.Sts2.Core.Helpers.EnergyIconHelper";

    private static bool _installed;
    private static readonly AsyncLocal<int> OwnedRuneRecognitionScopeDepth = new();

    private static Type? _randomForgeShopRelicType;
    private static MethodInfo? _tryCreateRandomForgeMethod;
    private static MethodInfo? _isHextechCustomRelicMethod;
    private static bool _resolvedHextechCatalogLookupMethods;

    public static void TryInstall(Harmony harmony)
    {
        if (_installed)
        {
            return;
        }

        ModCompatContext? context = ModCompat.TryCreate(HextechModId, "HextechRuntimeCompat");
        if (context == null)
        {
            return;
        }

        _installed = true;
        MainFile.Logger.Info("HextechRuntimeCompat: HextechRunes detected, applying Pig rune runtime integration");
        PatchHextechCatalog(harmony, context);
        PatchScopedRuntimeRecognition(harmony, context);
        PatchCompendiumDisplayCompat(harmony);
        RegisterShoppingCartForgeResolver(context);
    }

    public static void TryInstallIfAvailable()
    {
        TryInstall(new Harmony(MainFile.ModId));
    }

    private static void PatchHextechCatalog(Harmony harmony, ModCompatContext context)
    {
        Type? catalogType = context.ResolveType(HextechCatalogTypeName);
        if (catalogType == null)
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: HextechCatalog type not found");
            return;
        }

        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetAllSelectableRuneTypesPostfix), "GetAllSelectableRuneTypes");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetGenericSelectableRuneTypesPostfix), "GetGenericSelectableRuneTypes");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetGenericVisibleRuneTypesPostfix), "GetGenericVisibleRuneTypes");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetPlayerRuneTypesForRarityPostfix), "GetPlayerRuneTypesForRarity");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(IsPlayerRuneTypeSelectablePostfix), "IsPlayerRuneTypeSelectable");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetPlayerRunePoolKeyPostfix), "GetPlayerRunePoolKey");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(IsPlayerRuneAllowedInActPostfix), "IsPlayerRuneAllowedInAct");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetCharacterRuneGroupsPostfix), "GetCharacterRuneGroups");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetCanonicalForgesPostfix), "GetCanonicalForges");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetForgeTypesForRarityPostfix), "GetForgeTypesForRarity");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetCanonicalVisibleCustomRelicsPostfix), "GetCanonicalVisibleCustomRelics");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(IsAvailableForPlayerPostfix), "IsAvailableForPlayer");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(GetMutuallyExclusivePlayerRuneIdsPostfix), "GetMutuallyExclusivePlayerRuneIds");

        // NOTE: TryGetPlayerRuneRarity is intentionally NOT patched.
        // Harmony cannot safely handle the internal HextechRarityTier enum
        // in an 'out' parameter via 'ref object' in a postfix — doing so
        // causes a hard freeze during overlay dismissal.
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(IsHextechRelicScopedPostfix), "IsHextechRelic");
        context.PatchMethods(harmony, catalogType, typeof(HextechRuntimeCompat), null, nameof(IsHextechForgeRelicPostfix), "IsHextechForgeRelic");
    }

    private static void PatchScopedRuntimeRecognition(Harmony harmony, ModCompatContext context)
    {
        context.PatchMethods(
            harmony,
            context.ResolveType(HextechRuneGrantHelperTypeName),
            typeof(HextechRuntimeCompat),
            nameof(BeginOwnedRuneRecognitionScope),
            nameof(EndOwnedRuneRecognitionScope),
            "BuildObtainableRunePool",
            "ReplaceOwnedHextechRunesWithRandomRunes");
        context.PatchMethods(
            harmony,
            context.ResolveType(HextechRuneSelectionCoordinatorTypeName),
            typeof(HextechRuntimeCompat),
            nameof(BeginOwnedRuneRecognitionScope),
            nameof(EndOwnedRuneRecognitionScope),
            "BuildSelectableRunePool");
        context.PatchMethods(
            harmony,
            context.ResolveType(HextechMayhemActRecoveryTypeName),
            typeof(HextechRuntimeCompat),
            nameof(BeginOwnedRuneRecognitionScope),
            nameof(EndOwnedRuneRecognitionScope),
            "RecoverResolvedActs",
            "GetMinimumPlayerHexCount",
            "GetHighestActResolvedByPlayerRuneCounts",
            "TryInferRarityForActFromPlayerRelics",
            "DescribePlayerHexCounts");
        context.PatchMethods(
            harmony,
            context.ResolveType(HextechTelemetryTypeName),
            typeof(HextechRuntimeCompat),
            nameof(BeginOwnedRuneRecognitionScope),
            nameof(EndOwnedRuneRecognitionScope),
            "OnRunEnded");
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

    private static IReadOnlyList<RelicModel> GetPigForgeRelics()
    {
        return HextechForgeRegistry.GetAllForges()
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
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

    /// <summary>
    /// Inject shared (non-character-specific) pig runes into the generic visible
    /// rune type list so they appear in the relic compendium organized by rarity
    /// tier (Silver → Gold → Prismatic) rather than all lumped at the end.
    ///
    /// AllRuneTypes is built Silver → Gold → Prismatic at registration time, so
    /// we split the original list into thirds and append our shared runes to the
    /// matching tier. The positional heuristic is coarse but correct because the
    /// native registration order in HextechContentRegistry is guaranteed.
    /// </summary>
    public static void GetGenericVisibleRuneTypesPostfix(ref IReadOnlyList<Type> __result)
    {
        int total = __result.Count;
        if (total == 0)
        {
            __result = HextechPigRuneRegistry.GetSharedRuneTypes().ToArray();
            return;
        }

        // AllRuneTypes is ordered Silver → Gold → Prismatic. Split roughly.
        int silverEnd = Math.Max(1, total / 3);
        int goldEnd = Math.Max(silverEnd + 1, total * 2 / 3);

        List<Type> merged = [];
        for (int i = 0; i < total; i++)
        {
            merged.Add(__result[i]);
            if (i == silverEnd - 1)
                merged.AddRange(HextechPigRuneRegistry.GetSharedRunesByRarity(HextechRuneRarity.Silver));
            if (i == goldEnd - 1)
                merged.AddRange(HextechPigRuneRegistry.GetSharedRunesByRarity(HextechRuneRarity.Gold));
        }
        // Prismatic shared runes go at the very end
        merged.AddRange(HextechPigRuneRegistry.GetSharedRunesByRarity(HextechRuneRarity.Prismatic));

        __result = merged.ToArray();
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

    public static void GetCanonicalForgesPostfix(ref IReadOnlyList<RelicModel> __result)
    {
        __result = __result.Concat(GetPigForgeRelics()).Distinct().ToArray();
    }

    public static void GetForgeTypesForRarityPostfix(object rarity, ref IReadOnlyList<Type> __result)
    {
        string name = rarity.ToString() ?? string.Empty;
        IReadOnlyList<Type> pigForges = name switch
        {
            "Silver" => HextechForgeRegistry.GetForgesByRarity(HextechForgeRarity.Silver),
            "Gold" => HextechForgeRegistry.GetForgesByRarity(HextechForgeRarity.Gold),
            "Prismatic" => HextechForgeRegistry.GetForgesByRarity(HextechForgeRarity.Prismatic),
            _ => Array.Empty<Type>()
        };
        __result = __result.Concat(pigForges).Distinct().ToArray();
    }

    public static void GetCanonicalVisibleCustomRelicsPostfix(ref IReadOnlyList<RelicModel> __result)
    {
        __result = __result
            .Concat(HextechPigRuneRegistry.GetAllRunes().Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type))))
            .Concat(GetPigForgeRelics())
            .Distinct()
            .ToArray();
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
            __result = HextechPigRuneRegistry.IsAllowedInAct(runeType, actIndex, IsEndlessModeActive());
        }
    }

    public static void IsHextechRelicScopedPostfix(RelicModel? relic, ref bool __result)
    {
        if (!__result && IsOwnedRuneRecognitionScopeActive() && HextechPigRuneRegistry.IsPigOrSharedRune(relic))
        {
            __result = true;
        }
    }

    public static void IsHextechForgeRelicPostfix(RelicModel? relic, ref bool __result)
    {
        if (!__result && HextechForgeRegistry.IsPigForge(relic))
        {
            __result = true;
        }
    }

    public static void TryGetPlayerRuneRarityScopedPostfix(RelicModel? relic, ref bool __result, ref object rarity)
    {
        if (__result || !IsOwnedRuneRecognitionScopeActive() || !HextechPigRuneRegistry.TryGetRarity(relic, out HextechRuneRarity pigRarity))
        {
            return;
        }

        if (rarity == null)
        {
            return;
        }

        Type rarityType = rarity.GetType();
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
            .Concat(GetPigForgeRelics())
            .Distinct()
            .ToArray();
    }

    public static void IsRelicSeenPostfix(RelicModel relic, ref bool __result)
    {
        if (!__result && (HextechPigRuneRegistry.IsPigOrSharedRune(relic) || HextechForgeRegistry.IsPigForge(relic)))
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

    public static bool TryGetSafeEnergyPrefix(RelicModel? relic, out string prefix)
    {
        prefix = string.Empty;
        if (relic == null)
        {
            return false;
        }

        if (HextechPigRuneRegistry.IsPigOrSharedRune(relic))
        {
            prefix = ModelDb.CardPool<Characters.PigCardPool>().EnergyColorName;
            return true;
        }

        if (IsOfficialHextechCustomRelic(relic))
        {
            prefix = "red";
            return true;
        }

        return false;
    }

    private static bool HasPigCharacterGroup(object group)
    {
        PropertyInfo? keyProperty = group.GetType().GetProperty("LocalizationKey");
        return string.Equals(keyProperty?.GetValue(group) as string, "CHARACTER.PIG", StringComparison.Ordinal);
    }

    private static bool IsOfficialHextechCustomRelic(RelicModel relic)
    {
        EnsureHextechCatalogLookupMethodsResolved();
        if (_isHextechCustomRelicMethod == null)
        {
            return false;
        }

        try
        {
            return _isHextechCustomRelicMethod.Invoke(null, [relic]) as bool? == true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"HextechRuntimeCompat: failed to query IsHextechCustomRelic for {relic.Id.Entry}: {ex.Message}");
            return false;
        }
    }

    private static void EnsureHextechCatalogLookupMethodsResolved()
    {
        if (_resolvedHextechCatalogLookupMethods)
        {
            return;
        }

        _resolvedHextechCatalogLookupMethods = true;
        Type? catalogType = AccessTools.TypeByName(HextechCatalogTypeName);
        if (catalogType == null)
        {
            return;
        }

        _isHextechCustomRelicMethod = AccessTools.Method(catalogType, "IsHextechCustomRelic", [typeof(RelicModel)]);
    }

    private static void RegisterShoppingCartForgeResolver(ModCompatContext context)
    {
        _randomForgeShopRelicType = context.ResolveType(RandomForgeShopRelicTypeName);
        Type? forgeHelperType = context.ResolveType(HextechForgeGrantHelperTypeName);

        if (_randomForgeShopRelicType != null && forgeHelperType != null)
        {
            // Use GetMethods + filter to avoid AmbiguousMatchException if HextechRunes
            // adds multiple overloads of TryCreateRandomForge
            _tryCreateRandomForgeMethod = forgeHelperType
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .SingleOrDefault(m => m.Name == "TryCreateRandomForge" && m.GetParameters().Length == 3);
            if (_tryCreateRandomForgeMethod != null)
            {
                ShoppingCartManager.ResolveShopProxyRelic = ResolveRandomForgeProxy;
                MainFile.Logger.Info("HextechRuntimeCompat: Registered shopping cart forge resolver");
            }
            else
            {
                MainFile.Logger.Warn("HextechRuntimeCompat: TryCreateRandomForge method not found on HextechForgeGrantHelper");
            }
        }
        else
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: Could not resolve forge types for shopping cart resolver");
        }
    }

    private static bool IsEndlessModeActive()
    {
        try
        {
            RunState? state = RunManager.Instance?.DebugOnlyGetState();
            if (state == null)
            {
                return false;
            }

            return state.Modifiers.Any(modifier =>
                modifier.Id.Entry.Contains("ENDLESS", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private static Task<RelicModel?> ResolveRandomForgeProxy(RelicModel relicModel, Player player)
    {
        if (_randomForgeShopRelicType == null || _tryCreateRandomForgeMethod == null)
            return Task.FromResult<RelicModel?>(null);

        // Only handle RandomForgeShopRelic instances
        if (relicModel.GetType() != _randomForgeShopRelicType)
            return Task.FromResult<RelicModel?>(null);

        try
        {
            // HextechForgeGrantHelper.TryCreateRandomForge(Player player, Rng rng, out RelicModel? forge)
            // The out parameter value is written back into the argument array after invocation
            object?[] parameters = [player, player.PlayerRng.Shops, null];
            bool success = (bool)_tryCreateRandomForgeMethod.Invoke(null, parameters)!;
            if (!success || parameters[2] == null)
            {
                MainFile.Logger.Warn("HextechRuntimeCompat: TryCreateRandomForge returned no forge");
                return Task.FromResult<RelicModel?>(null);
            }

            return Task.FromResult((RelicModel?)parameters[2]);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"HextechRuntimeCompat: Failed to resolve random forge proxy: {ex.Message}");
            return Task.FromResult<RelicModel?>(null);
        }
    }

}
