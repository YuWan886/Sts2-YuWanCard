using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Hextech.Relics;

namespace YuWanCard.Hextech;

/// <summary>
/// Optional HextechRunes integration.
///
/// Player runes are NOT handled here: they are registered through HextechRunes' published
/// external-content API by <see cref="HextechRuneInteropBridge"/>, which keeps pool building,
/// the multiplayer selection protocol, the config menu and the compendium consistent.
/// HextechRunes intentionally keeps its catalog classes <c>internal</c> and asks integrations
/// not to patch them, so the Harmony patches left here only cover what the public API cannot
/// express: pig forges (HextechRunesInterop has no forge entry point) and the extra mutual
/// exclusions between pig and Hextech's own seven-sins runes.
/// </summary>
public static class HextechRuntimeCompat
{
    private const string HextechCatalogTypeName = "HextechRunes.HextechCatalog";
    private const string UnlockStateTypeName = "MegaCrit.Sts2.Core.Unlocks.UnlockState";
    private const string SaveManagerTypeName = "MegaCrit.Sts2.Core.Saves.SaveManager";
    private const string EnergyIconHelperTypeName = "MegaCrit.Sts2.Core.Helpers.EnergyIconHelper";

    private static bool _installed;

    private static MethodInfo? _isHextechCustomRelicMethod;
    private static bool _resolvedHextechCatalogLookupMethods;

    public static void TryInstall(Harmony harmony)
    {
        // Rune registration is independent of the patch installation below: it must run during
        // mod initialization even when HextechRunes has not been loaded yet (it hooks
        // AssemblyLoad in that case).
        HextechRuneInteropBridge.TryRegister();

        if (_installed)
        {
            return;
        }

        // HextechRunes loads one of several version-specific variant DLLs, so resolve its types
        // by name across all loaded assemblies (its assembly name, not its manifest id, is stable).
        Type? catalogType = AccessTools.TypeByName(HextechCatalogTypeName);
        if (catalogType == null)
        {
            return;
        }

        _installed = true;
        MainFile.Logger.Info("HextechRuntimeCompat: HextechRunes detected, applying Pig forge runtime integration");
        PatchHextechCatalog(harmony, catalogType);
        PatchCompendiumDisplayCompat(harmony);
        PatchForgeStacking(harmony);
    }

    public static void TryInstallIfAvailable()
    {
        TryInstall(new Harmony(MainFile.ModId));
    }

    private static void PatchHextechCatalog(Harmony harmony, Type catalogType)
    {
        // Pig forges cannot go through HextechRunesInterop (it only exposes player runes), so
        // they are injected into Hextech's own forge catalog here: the grant pool
        // (GetForgeTypesForRarity), the compendium/series lists and the availability filter.
        PatchMethod(harmony, catalogType, "GetForgeTypesForRarity", null, nameof(GetForgeTypesForRarityPostfix));
        PatchMethod(harmony, catalogType, "GetCanonicalForges", null, nameof(GetCanonicalForgesPostfix));
        PatchMethod(harmony, catalogType, "GetCanonicalVisibleCustomRelics", null, nameof(GetCanonicalVisibleCustomRelicsPostfix));
        PatchMethod(harmony, catalogType, "IsAvailableForPlayer", null, nameof(IsAvailableForPlayerPostfix));
        PatchMethod(harmony, catalogType, "IsHextechForgeRelic", null, nameof(IsHextechForgeRelicPostfix));
        // Seven-sins exclusivity spans pig runes and Hextech's own runes, so it cannot be
        // expressed with the per-rune registration flags.
        PatchMethod(harmony, catalogType, "GetMutuallyExclusivePlayerRuneIds", null, nameof(GetMutuallyExclusivePlayerRuneIdsPostfix));
    }

    private static void PatchMethod(Harmony harmony, Type targetType, string methodName, string? prefixName, string? postfixName)
    {
        MethodInfo? original = AccessTools.Method(targetType, methodName);
        MethodInfo? prefix = prefixName == null ? null : AccessTools.Method(typeof(HextechRuntimeCompat), prefixName);
        MethodInfo? postfix = postfixName == null ? null : AccessTools.Method(typeof(HextechRuntimeCompat), postfixName);
        if (original == null || (prefixName != null && prefix == null) || (postfixName != null && postfix == null))
        {
            MainFile.Logger.Warn($"HextechRuntimeCompat: skipped patch {targetType.Name}.{methodName}");
            return;
        }

        harmony.Patch(
            original,
            prefix == null ? null : new HarmonyMethod(prefix),
            postfix == null ? null : new HarmonyMethod(postfix));
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

    /// <summary>
    /// Make pig forges stack like vanilla Hextech forges. Hextech's own RelicCmd.Obtain hook
    /// only merges duplicates for relics that are its internal HextechForgeBase; pig forges are
    /// a separate type, so we install a parallel prefix that increments the owned forge's stack
    /// count instead of adding a second copy.
    /// </summary>
    private static void PatchForgeStacking(Harmony harmony)
    {
        MethodInfo? obtainMethod = AccessTools.Method(
            typeof(RelicCmd),
            nameof(RelicCmd.Obtain),
            [typeof(RelicModel), typeof(Player), typeof(int)]);
        MethodInfo? prefix = AccessTools.Method(typeof(HextechRuntimeCompat), nameof(PigForgeObtainPrefix));
        if (obtainMethod != null && prefix != null)
        {
            harmony.Patch(obtainMethod, prefix: new HarmonyMethod(prefix));
        }
        else
        {
            MainFile.Logger.Warn("HextechRuntimeCompat: skipped patch RelicCmd.Obtain for pig forge stacking");
        }
    }

    public static bool PigForgeObtainPrefix(RelicModel relic, Player player, ref Task<RelicModel> __result)
    {
        if (relic is not HextechPigForgeBase)
        {
            return true;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        HextechPigForgeBase? ownedForge = player.Relics
            .OfType<HextechPigForgeBase>()
            .FirstOrDefault(owned => (owned.CanonicalInstance?.Id ?? owned.Id) == id);
        if (ownedForge == null || ReferenceEquals(ownedForge, relic))
        {
            return true;
        }

        player.RunState.CurrentMapPointHistoryEntry?
            .GetEntry(player.NetId)
            .RelicChoices
            .Add(new ModelChoiceHistoryEntry(relic.Id, wasPicked: true));
        SaveManager.Instance.MarkRelicAsSeen(relic);
        __result = ObtainStackedPigForge(ownedForge);
        return false;
    }

    private static async Task<RelicModel> ObtainStackedPigForge(HextechPigForgeBase ownedForge)
    {
        ownedForge.AddForgeStack(flash: !ownedForge.HasUponPickupEffect);
        await ownedForge.AfterObtained();
        return ownedForge;
    }

    private static IReadOnlyList<RelicModel> GetPigForgeRelics()
    {
        return HextechForgeRegistry.GetAllForges()
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
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
        __result = __result.Concat(GetPigForgeRelics()).Distinct().ToArray();
    }

    public static void IsAvailableForPlayerPostfix(RelicModel relic, Player player, ref bool __result)
    {
        if (HextechForgeRegistry.IsPigForge(relic))
        {
            __result = HextechForgeRegistry.IsAvailableForPlayer(relic, player);
            if (__result && relic is HextechPigForgeBase pigForge)
            {
                __result = pigForge.IsAvailableForPlayer(player);
            }
        }
    }

    public static void IsHextechForgeRelicPostfix(RelicModel? relic, ref bool __result)
    {
        if (!__result && HextechForgeRegistry.IsPigForge(relic))
        {
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
        // Player runes are already listed by HextechRunes' own inspect/compendium patches now
        // that they are registered through its API; only pig forges need adding here.
        __result = (__result ?? Array.Empty<RelicModel>())
            .Concat(GetPigForgeRelics())
            .Distinct()
            .ToArray();
    }

    public static void IsRelicSeenPostfix(RelicModel relic, ref bool __result)
    {
        if (!__result && HextechForgeRegistry.IsPigForge(relic))
        {
            __result = true;
        }
    }

    public static void EnergyIconHelperGetPrefixPostfix(AbstractModel model, ref string __result)
    {
        // HextechRunes' own postfix already assigns the prefix for everything in its registry
        // (which now includes the pig runes); only pig forges are ours to label.
        if (model is RelicModel relic && HextechForgeRegistry.IsPigForge(relic))
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
}
