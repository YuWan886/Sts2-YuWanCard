using System.Reflection;
using HarmonyLib;

namespace YuWanCard.Core.Patching;

/// <summary>
/// Owns a Harmony instance with named patch registration, application,
/// and rollback. Critical failures trigger full unpatch.
/// </summary>
public class ModPatcher
{
    private readonly Harmony _harmony;
    private readonly List<ModPatchInfo> _registered = [];
    private readonly Dictionary<string, bool> _applied = [];
    private bool _isApplied;

    public ModPatcher(string harmonyId)
    {
        _harmony = new Harmony(harmonyId);
    }

    public bool IsApplied => _isApplied;

    /// <summary>
    /// Register a Harmony patch class that implements IPatchMethod.
    /// </summary>
    public void RegisterPatch<T>() where T : IPatchMethod, new()
    {
        if (_isApplied)
            throw new InvalidOperationException("Cannot register after PatchAll");

        var info = ModPatchInfo.FromMethod<T>();
        if (_registered.Any(p => p.Id == info.Id))
            return;

        _registered.Add(info);
        MainFile.Logger.Debug($"[Patcher] Registered: {info.Id}");
    }

    /// <summary>
    /// Register a plain Harmony patch class by type.
    /// </summary>
    public void RegisterPatchType(Type patchType, string? id = null, bool isCritical = true)
    {
        if (_isApplied)
            throw new InvalidOperationException("Cannot register after PatchAll");

        var patchId = id ?? patchType.Name;
        if (_registered.Any(p => p.Id == patchId))
            return;

        _registered.Add(new ModPatchInfo
        {
            Id = patchId,
            Description = patchType.Name,
            PatchType = patchType,
            IsCritical = isCritical
        });
    }

    /// <summary>
    /// Applies all registered patches. On critical failure, rolls back all.
    /// Returns false if any critical patch failed.
    /// </summary>
    public bool PatchAll()
    {
        if (_isApplied)
            return true;

        MainFile.Logger.Info($"[Patcher] Applying {_registered.Count} patches...");

        int success = 0, criticalFailed = 0;

        foreach (var info in _registered)
        {
            try
            {
                _harmony.CreateClassProcessor(info.PatchType).Patch();
                _applied[info.Id] = true;
                success++;
            }
            catch (Exception ex)
            {
                _applied[info.Id] = false;
                if (info.IsCritical) criticalFailed++;
                MainFile.Logger.Warn($"[Patcher] {info.Id} failed: {ex.Message}");
            }
        }

        MainFile.Logger.Info(
            $"[Patcher] {success}/{_registered.Count} applied, {criticalFailed} critical failures");

        if (criticalFailed > 0)
        {
            MainFile.Logger.Error("[Patcher] Critical failures — rolling back");
            UnpatchAll();
            return false;
        }

        _isApplied = true;
        return true;
    }

    /// <summary>
    /// Apply a single patch with individual try/catch (mobile-compatible).
    /// </summary>
    public void ApplySingle(Action<Harmony> apply, string id)
    {
        try
        {
            apply(_harmony);
            _applied[id] = true;
        }
        catch (Exception ex)
        {
            _applied[id] = false;
            MainFile.Logger.Warn($"[Patcher] {id} failed (may be mobile): {ex.Message}");
        }
    }

    /// <summary>
    /// Safely patches all [HarmonyPatch] classes in the assembly individually,
    /// so that one failing method doesn't prevent others from being applied.
    /// Essential for Android/Mono AOT compatibility.
    /// </summary>
    /// <param name="assembly">Assembly to scan for [HarmonyPatch] types.</param>
    /// <param name="excludeTypeNames">Optional set of type names to skip
    /// (e.g. patches that must be applied conditionally by platform).</param>
    /// <returns>Number of successfully applied patch classes.</returns>
    public int PatchAllSafe(Assembly assembly, HashSet<string>? excludeTypeNames = null)
    {
        if (_isApplied)
            return 0;

        var types = assembly.GetTypes();
        int success = 0;
        int failed = 0;

        foreach (var type in types)
        {
            // Only process types with [HarmonyPatch] attribute(s)
            if (!type.GetCustomAttributes<HarmonyPatch>().Any())
                continue;

            // Skip types that are applied manually with platform checks
            if (excludeTypeNames != null && excludeTypeNames.Contains(type.Name))
                continue;

            try
            {
                _harmony.CreateClassProcessor(type).Patch();
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                MainFile.Logger.Warn(
                    $"[Patcher] Patch class '{type.Name}' failed (may be mobile): {ex.Message}");
            }
        }

        MainFile.Logger.Info(
            $"[Patcher] BulkPatchAllSafe: {success} applied, {failed} failed, {success + failed} total");

        if (failed == 0)
        {
            _isApplied = true;
        }

        return success;
    }

    public void UnpatchAll()
    {
        _harmony.UnpatchAll(_harmony.Id);
        _isApplied = false;
    }
}
