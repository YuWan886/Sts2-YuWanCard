namespace YuWanCard.Core.Patching;

/// <summary>
/// Metadata for a single Harmony patch registered with ModPatcher.
/// </summary>
public sealed class ModPatchInfo
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Type PatchType { get; init; } = null!;
    public bool IsCritical { get; init; } = true;

    public static ModPatchInfo FromMethod<T>() where T : IPatchMethod, new()
    {
        var instance = new T();
        return new ModPatchInfo
        {
            Id = instance.Id,
            Description = instance.Description,
            PatchType = typeof(T),
            IsCritical = instance.IsCritical
        };
    }
}
