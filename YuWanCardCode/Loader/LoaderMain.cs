using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard.Loader;

/// <summary>
///     Entry assembly for the multi-version YuWanCard bundle. It loads the matching
///     <c>YuWanCard.Content.dll</c> from <c>lib/&lt;compat&gt;/</c> into the default ALC,
///     bridges its types into <c>ReflectionHelper.ModTypes</c>, then invokes the real
///     content initializer.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class LoaderMain
{
    public const string ModId = "YuWanCard";

    private static readonly Lock VariantAssembliesLock = new();
    private static readonly List<Assembly> VariantAssemblies = [];
    private static bool _bridgePatched;

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    /// <summary>The loaded content-variant assembly, or <see langword="null" /> when loading failed.</summary>
    public static Assembly? LoadedVariantAssembly { get; private set; }

    /// <summary>Compat target of the selected variant, or <see langword="null" />.</summary>
    public static Version? SelectedVariantVersion { get; private set; }

    /// <summary>Detected host game version, or <see langword="null" /> when unavailable.</summary>
    public static Version? HostVersion { get; private set; }

    /// <summary>Types of all registered content-variant assemblies, for the ModTypes bridge.</summary>
    internal static Type[] GetVariantModTypes()
    {
        Assembly[] assemblies;
        lock (VariantAssembliesLock)
        {
            assemblies = [.. VariantAssemblies];
        }

        return [.. assemblies.SelectMany(GetLoadableTypes).Distinct()];
    }

    public static void Initialize()
    {
        var loaderDir = Path.GetDirectoryName(typeof(LoaderMain).Assembly.Location);
        if (string.IsNullOrEmpty(loaderDir))
        {
            Logger.Error("[Loader] Could not resolve loader directory.");
            return;
        }

        // Install the ModTypes bridge and the PatchAll guard before anything can
        // access ReflectionHelper.ModTypes (the game does that during ModelDb.Init).
        EnsureBridgePatch();

        var host = LoaderHostVersion.Numeric;
        HostVersion = host;
        var hostLabel = LoaderHostVersion.ReleaseLabel;
        var picked = LoaderVariantBundle.PickVariant(loaderDir, host);
        if (picked is null)
        {
            Logger.Error(
                $"[Loader] No compatible variant under {loaderDir}/lib (host={(hostLabel ?? host?.ToString()) ?? "unknown"}).");
            return;
        }

        Logger.Info(
            $"[Loader] Host version label={hostLabel ?? "<none>"} numeric={host?.ToString() ?? "<none>"}; picked variant {picked.CompatTarget}.");

        if (!File.Exists(picked.DllPath))
        {
            Logger.Error($"[Loader] Variant folder missing content DLL: {picked.DllPath}");
            return;
        }

        Assembly contentAssembly;
        try
        {
            var alc = AssemblyLoadContext.GetLoadContext(typeof(LoaderMain).Assembly) ?? AssemblyLoadContext.Default;
            contentAssembly = alc.LoadFromAssemblyPath(picked.DllPath);
            RegisterVariantAssembly(contentAssembly);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Loader] Failed to load {picked.DllPath}: {ex}");
            return;
        }

        LoadedVariantAssembly = contentAssembly;
        SelectedVariantVersion = picked.Version;
        AssociateVariantAssemblyWithMod(contentAssembly);

        try
        {
            InvokeRealInitializer(contentAssembly);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Loader] Failed to initialize content: {ex}");
        }
    }

    private static void RegisterVariantAssembly(Assembly contentAssembly)
    {
        lock (VariantAssembliesLock)
        {
            if (VariantAssemblies.Any(assembly => string.Equals(
                    assembly.Location,
                    contentAssembly.Location,
                    StringComparison.OrdinalIgnoreCase)))
                return;

            VariantAssemblies.Add(contentAssembly);
        }
    }

    /// <summary>
    ///     Associates the hand-loaded content-variant assembly with the <c>YuWanCard</c> mod so the
    ///     multiplayer ID sorter (<c>AssemblyInfo</c>/<c>ContentSorter</c>, 0.109+) can attribute its
    ///     model types. Without this, every content type logs "not associated with any mod" and the
    ///     ID sort loses determinism across peers. Pre-0.109 hosts scan <c>mod.assembly</c> directly
    ///     (bridged by <c>ReflectionHelperGetSubtypesFromAssemblyPatch</c>) and expose no association
    ///     API, so the reflection below simply no-ops there.
    /// </summary>
    private static void AssociateVariantAssemblyWithMod(Assembly contentAssembly)
    {
        var associateMethod = typeof(ModManager).GetMethod(
            "AssociateAssemblyWithMod",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), typeof(Assembly)],
            null);
        if (associateMethod is null)
        {
            Logger.Info(
                $"[Loader] Host exposes no ModManager.AssociateAssemblyWithMod; skipping association for {contentAssembly.GetName().Name}.");
            return;
        }

        try
        {
            associateMethod.Invoke(null, [ModId, contentAssembly]);
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Loader] ModManager.AssociateAssemblyWithMod failed: {ex.Message}");
        }

        if (IsAssemblyAssociatedWithMod(contentAssembly))
        {
            Logger.Info($"[Loader] Associated content assembly {contentAssembly.GetName().Name} with mod {ModId}.");
            return;
        }

        if (TryAppendToModAssemblies(contentAssembly))
        {
            Logger.Info(
                $"[Loader] Associated content assembly {contentAssembly.GetName().Name} with mod {ModId} (fallback).");
            return;
        }

        Logger.Warn(
            $"[Loader] Could not associate {contentAssembly.GetName().Name} with mod {ModId}; relying on the reflection bridge for type discovery.");
    }

    private static bool IsAssemblyAssociatedWithMod(Assembly contentAssembly)
    {
        List<Assembly>? assemblies = GetModAssemblies();
        return assemblies is not null &&
               assemblies.Any(assembly => ReferenceEquals(assembly, contentAssembly));
    }

    private static bool TryAppendToModAssemblies(Assembly contentAssembly)
    {
        List<Assembly>? assemblies = GetModAssemblies();
        if (assemblies is null)
        {
            return false;
        }

        if (assemblies.Any(assembly => ReferenceEquals(assembly, contentAssembly)))
        {
            return true;
        }

        assemblies.Add(contentAssembly);
        return true;
    }

    /// <summary>
    ///     The <c>Mod.assemblies</c> field is beta-only (public in 0.109+), so it is resolved via
    ///     reflection to keep this loader compiling against hosts that predate it.
    /// </summary>
    private static List<Assembly>? GetModAssemblies()
    {
        foreach (Mod mod in ModManager.Mods)
        {
            if (!string.Equals(mod.manifest?.id, ModId, StringComparison.Ordinal))
            {
                continue;
            }

            var field = typeof(Mod).GetField(
                "assemblies",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(mod) as List<Assembly>;
        }

        return null;
    }

    private static void EnsureBridgePatch()
    {
        if (_bridgePatched)
            return;

        HarmonyPatchAllTypeLoadGuard.Install(message => Logger.Warn("[Loader] " + message));

        var harmony = new Harmony("YuWanCard.Loader.ReflectionBridge");
        harmony.PatchAll(typeof(LoaderMain).Assembly);
        _bridgePatched = true;
    }

    private static void InvokeRealInitializer(Assembly contentAssembly)
    {
        Type[] types;
        try
        {
            types = contentAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Logger.Error($"[Loader] ReflectionTypeLoadException while scanning {contentAssembly.FullName}: {ex}");
            foreach (var t in ex.Types.Where(static x => x is not null))
                TryInvokeInitializerOnType(t!);

            return;
        }

        if (types.Any(TryInvokeInitializerOnType))
            return;

        Logger.Error($"[Loader] No type with {nameof(ModInitializerAttribute)} found in {contentAssembly.FullName}.");
    }

    private static bool TryInvokeInitializerOnType(Type type)
    {
        var attr = type.GetCustomAttribute<ModInitializerAttribute>();
        if (attr is null)
            return false;

        var method = type.GetMethod(attr.initializerMethod,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (method is null)
        {
            Logger.Error(
                $"[Loader] Type {type.FullName} has {nameof(ModInitializerAttribute)} but no static method {attr.initializerMethod}.");
            return false;
        }

        method.Invoke(null, null);
        return true;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaderExceptions = ex.LoaderExceptions.OfType<Exception>().ToArray();
            var details = string.Join(
                Environment.NewLine,
                loaderExceptions.Take(8).Select(static exception => exception.ToString()));
            var detailBlock = details.Length > 0 ? Environment.NewLine + details : string.Empty;
            var omitted = loaderExceptions.Length > 8
                ? $"{Environment.NewLine}... {loaderExceptions.Length - 8} more loader exception(s) omitted."
                : string.Empty;
            Logger.Warn(
                $"[Loader] Partial type load for {assembly.FullName}: {ex.Message}" +
                $"{detailBlock}{omitted}");
            return ex.Types.OfType<Type>();
        }
    }
}
