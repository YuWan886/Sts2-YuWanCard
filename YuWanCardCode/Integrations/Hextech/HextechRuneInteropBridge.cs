using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Relics;

namespace YuWanCard.Hextech;

/// <summary>
/// Registers every pig rune through HextechRunes' published external-content API
/// (<c>HextechRunes.HextechRunesInterop</c>, ApiVersion 1) instead of Harmony-patching
/// Hextech internals. Registration is what puts the runes into
/// <c>HextechCatalog</c>'s player-rune metadata, so pool building, the multiplayer
/// selection protocol, the config menu and the compendium all agree on the same set.
///
/// See HextechRunes' INTEGRATION.md. HextechRunes is an optional dependency, so the
/// whole bridge is reflection-only and a no-op when the mod is absent.
/// </summary>
public static class HextechRuneInteropBridge
{
    /// <summary>Assembly name is fixed across HextechRunes variants (including derivative builds).</summary>
    public const string HextechAssemblyName = "HextechRunes";

    private const string InteropTypeName = "HextechRunes.HextechRunesInterop";
    private const string AssetModId = "YuWanCard";
    private const string DefaultTagKey = "COMPREHENSIVE";
    private const string ConfigSectionTitleKey = "YUWANCARD_HEXTECH_SECTION";
    private const int RequiredApiVersion = 1;

    private static readonly object Gate = new();
    private static readonly Dictionary<Type, RelicModel> CanonicalRuneCache = [];

    private static bool _registrationAttempted;
    private static bool _assemblyLoadHookInstalled;

    /// <summary>
    /// Registers pig runes with HextechRunes if it is loaded. Safe to call repeatedly and from
    /// any mod-initialization entry point; the actual registration runs at most once.
    /// </summary>
    public static void TryRegister()
    {
        lock (Gate)
        {
            if (_registrationAttempted)
            {
                return;
            }

            Assembly? hextechAssembly = FindHextechAssembly();
            if (hextechAssembly == null)
            {
                InstallAssemblyLoadHook();
                return;
            }

            _registrationAttempted = true;
            try
            {
                RegisterWithAssembly(hextechAssembly);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"HextechRuneInteropBridge: pig rune registration aborted: {ex}");
            }
        }
    }

    private static Assembly? FindHextechAssembly()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => !assembly.IsDynamic
                && string.Equals(assembly.GetName().Name, HextechAssemblyName, StringComparison.Ordinal));
    }

    private static void InstallAssemblyLoadHook()
    {
        if (_assemblyLoadHookInstalled)
        {
            return;
        }

        _assemblyLoadHookInstalled = true;
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        if (!string.Equals(args.LoadedAssembly.GetName().Name, HextechAssemblyName, StringComparison.Ordinal))
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        TryRegister();
    }

    private static void RegisterWithAssembly(Assembly hextechAssembly)
    {
        Type? interopType = hextechAssembly.GetType(InteropTypeName);
        if (interopType == null)
        {
            MainFile.Logger.Info(
                "HextechRuneInteropBridge: HextechRunesInterop not found (HextechRunes is older than 0.9.7); pig runes stay unregistered.");
            return;
        }

        object? apiVersionValue = interopType
            .GetProperty("ApiVersion", BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null);
        if (apiVersionValue is not int apiVersion || apiVersion < RequiredApiVersion)
        {
            MainFile.Logger.Warn(
                $"HextechRuneInteropBridge: HextechRunesInterop.ApiVersion is {apiVersionValue ?? "<missing>"}, need >= {RequiredApiVersion}; skipped pig rune registration");
            return;
        }

        MethodInfo? registerPlayerRune = interopType.GetMethod(
            "RegisterPlayerRune",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [
                typeof(Type), typeof(string), typeof(string), typeof(string),
                typeof(int), typeof(string), typeof(string), typeof(Func<Player, bool>)
            ],
            null);
        if (registerPlayerRune == null)
        {
            MainFile.Logger.Warn("HextechRuneInteropBridge: HextechRunesInterop.RegisterPlayerRune not found; skipped pig rune registration");
            return;
        }

        int registered = 0;
        int failed = 0;
        foreach (HextechPigRuneRegistry.HextechRuneDefinition definition in HextechPigRuneRegistry.GetDefinitions())
        {
            try
            {
                Type runeType = definition.RuneType;
                Func<Player, bool> isAvailableForPlayer = player => IsRuneAvailableForPlayer(runeType, player);
                registerPlayerRune.Invoke(null,
                [
                    runeType,
                    definition.RarityName,
                    definition.FlagsName,
                    // PlayerRuneCharacterPool has no Pig member, so Pig-only runes are expressed
                    // with the availability predicate below instead. They therefore count as
                    // generic for Hextech's character-weight bias and compendium grouping; every
                    // grant path still filters through that predicate, so nothing leaks.
                    null,
                    0,
                    DefaultTagKey,
                    AssetModId,
                    isAvailableForPlayer
                ]);
                registered++;
            }
            catch (TargetInvocationException ex)
            {
                failed++;
                MainFile.Logger.Warn(
                    $"HextechRuneInteropBridge: failed to register {definition.RuneType.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                failed++;
                MainFile.Logger.Warn($"HextechRuneInteropBridge: failed to register {definition.RuneType.Name}: {ex.Message}");
            }
        }

        ApplyPoolLabels(interopType);
        RegisterConfigSectionTitle(interopType);

        MainFile.Logger.Info(
            $"HextechRuneInteropBridge: registered {registered} pig rune(s) with HextechRunes interop (api={apiVersion}, failed={failed})");
    }

    /// <summary>
    /// Source pill on the selection screen and the grouping key in the config menu. Hextech reads
    /// the text from HEXTECH_POOL.&lt;key&gt; in the relic_collection table.
    /// </summary>
    private static void ApplyPoolLabels(Type interopType)
    {
        MethodInfo? setPoolLabel = interopType.GetMethod(
            "SetPlayerRunePoolLabel",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(Type), typeof(string)],
            null);
        if (setPoolLabel == null)
        {
            return;
        }

        foreach (HextechPigRuneRegistry.HextechRuneDefinition definition in HextechPigRuneRegistry.GetDefinitions())
        {
            try
            {
                setPoolLabel.Invoke(null, [definition.RuneType, definition.PoolKey]);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"HextechRuneInteropBridge: failed to set pool label for {definition.RuneType.Name}: {ex.Message}");
            }
        }
    }

    private static void RegisterConfigSectionTitle(Type interopType)
    {
        MethodInfo? registerTitle = interopType.GetMethod(
            "RegisterConfigSectionTitle",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), typeof(string)],
            null);
        if (registerTitle == null)
        {
            return;
        }

        try
        {
            registerTitle.Invoke(null, [AssetModId, ConfigSectionTitleKey]);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"HextechRuneInteropBridge: failed to register config section title: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs on both ends of a multiplayer run, so it may only read synchronized state.
    /// Mirrors the rune's own <c>IsAvailableForPlayer</c> override (e.g. the shopping cart rune
    /// additionally requires an owned ShoppingCart).
    /// </summary>
    private static bool IsRuneAvailableForPlayer(Type runeType, Player player)
    {
        try
        {
            RelicModel? relic = GetCanonicalRune(runeType);
            return relic switch
            {
                HextechPigRuneBase pigRune => pigRune.IsAvailableForPlayer(player),
                HextechSharedRuneBase sharedRune => sharedRune.IsAvailableForPlayer(player),
                // Fail closed: every registered rune derives from one of the two bases above, so an
                // unresolved model must not be offered to anyone. Both peers reach the same verdict.
                _ => false
            };
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"HextechRuneInteropBridge: availability check for {runeType.Name} threw; rune excluded: {ex.Message}");
            return false;
        }
    }

    private static RelicModel? GetCanonicalRune(Type runeType)
    {
        lock (Gate)
        {
            if (CanonicalRuneCache.TryGetValue(runeType, out RelicModel? cached))
            {
                return cached;
            }

            // Only successful lookups are cached: ModelDb.GetById either returns the canonical
            // instance or throws, and a transient failure must not permanently exclude the rune.
            RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(runeType));
            CanonicalRuneCache[runeType] = relic;
            return relic;
        }
    }
}
