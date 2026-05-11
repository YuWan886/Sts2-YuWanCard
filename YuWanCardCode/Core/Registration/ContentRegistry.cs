using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard.Core.Registration;

/// <summary>
/// Scans assemblies for models with registration attributes and registers
/// them with the game's systems. Uses safe type scanning (catches
/// ReflectionTypeLoadException on Android/Mono). Supports freeze to
/// prevent late registrations after ModelDb.Init.
///
/// Registration flow:
///   1. RegisterAll scans types, calls ModHelper.AddModelToPool for [Pool],
///      and tracks attributed types for later canonical registration.
///   2. InitDeDuplicationPatch creates canonical instances during ModelDb.Init
///      and registers them with CustomEventRegistry / CustomAncientRegistry / etc.
///   3. Freeze() is called — further AddModel calls are no-ops.
/// </summary>
public static class ContentRegistry
{
    private static bool _initialized;
    private static bool _frozen;
    private static readonly object _lock = new();

    // Types tracked by registration attributes — consumed by InitDeDuplicationPatch
    // to register canonical instances created during ModelDb.Init.
    internal static readonly HashSet<Type> AncientTypes = [];
    internal static readonly HashSet<Type> OrbTypes = [];
    internal static readonly HashSet<Type> MonsterTypes = [];
    internal static readonly HashSet<Type> EnchantmentTypes = [];
    internal static readonly HashSet<Type> SingletonTypes = [];
    internal static readonly HashSet<Type> CharacterTypes = [];
    internal static readonly HashSet<Type> EventTypes = [];
    internal static readonly HashSet<Type> RelicPoolTypes = [];

    public static bool IsFrozen
    {
        get { lock (_lock) return _frozen; }
    }

    /// <summary>
    /// Freeze registrations. After this, AddModel logs a warning and skips.
    /// Called at the end of InitDeDuplicationPatch.SafeInit.
    /// </summary>
    public static void Freeze()
    {
        lock (_lock)
        {
            if (_frozen) return;
            _frozen = true;
        }
        MainFile.Logger.Info("ContentRegistry: frozen");
    }

    /// <summary>
    /// Automatically registers all content from all loaded assemblies.
    /// Called once during mod initialization.
    /// </summary>
    public static void AutoRegisterAll()
    {
        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                RegisterAll(assembly);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"Failed to register from {assembly.GetName().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Scans one assembly for [Pool] and other registration attributes.
    /// Uses safe type loading for Android/Mono compatibility.
    /// </summary>
    public static void RegisterAll(Assembly assembly)
    {
        int poolCount = 0, attrCount = 0;

        foreach (var type in AssemblyScanner.GetLoadableTypes(assembly))
        {
            if (type.IsAbstract) continue;

            // [Pool] attribute — register with game's pool system immediately
            var poolAttr = type.GetCustomAttribute<PoolAttribute>();
            if (poolAttr != null)
            {
                ModHelper.AddModelToPool(poolAttr.PoolType, type);
                poolCount++;
                continue;
            }

            // Collect types with explicit registration attributes.
            // Canonical instances are registered later in InitDeDuplicationPatch.
            if (type.HasAttribute<RegisterAncientAttribute>())
                { AncientTypes.Add(type); attrCount++; }
            if (type.HasAttribute<RegisterOrbAttribute>())
                { OrbTypes.Add(type); attrCount++; }
            if (type.HasAttribute<RegisterMonsterAttribute>())
                { MonsterTypes.Add(type); attrCount++; }
            if (type.HasAttribute<RegisterEnchantmentAttribute>())
                { EnchantmentTypes.Add(type); attrCount++; }
            if (type.HasAttribute<RegisterSingletonAttribute>())
                { SingletonTypes.Add(type); attrCount++; }
            if (type.HasAttribute<RegisterCharacterAttribute>())
                { CharacterTypes.Add(type); attrCount++; }
            if (type.HasAttribute<RegisterEventAttribute>())
                { EventTypes.Add(type); attrCount++; }

            // Backward compat: auto-detect events without explicit [RegisterEvent]
            if (typeof(EventModel).IsAssignableFrom(type) &&
                !typeof(AncientEventModel).IsAssignableFrom(type) &&
                !type.HasAttribute<RegisterEventAttribute>())
            {
                EventTypes.Add(type);
                attrCount++;
            }

            // Auto-detect custom relic pools (extend RelicPoolModel + implement IYuWanContent)
            if (typeof(RelicPoolModel).IsAssignableFrom(type) &&
                typeof(IYuWanContent).IsAssignableFrom(type))
            {
                RelicPoolTypes.Add(type);
                attrCount++;
            }
        }

        if (poolCount > 0 || attrCount > 0)
            MainFile.Logger.Info(
                $"ContentRegistry [{assembly.GetName().Name}]: {poolCount} pools, {attrCount} attributed");
    }

    /// <summary>
    /// Per-constructor registration. Called from base class constructors
    /// (YuWanRelicModel, YuWanPotionModel) for auto-registration.
    /// After freeze, logs a warning and skips.
    /// </summary>
    public static void AddModel(Type modelType)
    {
        if (IsFrozen)
        {
            MainFile.Logger.Warn(
                $"ContentRegistry: AddModel called after freeze for {modelType.Name}");
            return;
        }

        var poolAttr = modelType.GetCustomAttribute<PoolAttribute>();
        if (poolAttr != null)
            ModHelper.AddModelToPool(poolAttr.PoolType, modelType);
    }

    /// <summary>
    /// Convenience helper — checks for an attribute without allocating.
    /// </summary>
    private static bool HasAttribute<T>(this Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>() != null;
    }
}
