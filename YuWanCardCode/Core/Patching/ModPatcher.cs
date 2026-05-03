using HarmonyLib;

namespace YuWanCard.Core.Patching;

/// <summary>
/// Owns a Harmony instance with named patch registration, application,
/// and rollback. Critical failures trigger full unpatch.
/// Inspired by RitsuLib's ModPatcher system.
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

    public void UnpatchAll()
    {
        _harmony.UnpatchAll(_harmony.Id);
        _isApplied = false;
    }
}
