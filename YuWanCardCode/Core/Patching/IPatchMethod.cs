namespace YuWanCard.Core.Patching;

/// <summary>
/// Interface for self-describing Harmony patches. Implement on patch classes
/// to enable organized registration with metadata.
/// </summary>
public interface IPatchMethod
{
    string Id { get; }
    string Description { get; }
    bool IsCritical { get; }

    /// <summary>
    /// Return false to skip this patch on the current platform (e.g. Android/iOS).
    /// </summary>
    bool IsPlatformRelevant() => true;
}
