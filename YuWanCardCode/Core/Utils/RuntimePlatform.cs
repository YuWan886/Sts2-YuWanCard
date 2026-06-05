using Godot;

namespace YuWanCard;

/// <summary>
/// Provides platform detection utilities for conditional behavior across different runtimes.
/// Use this instead of direct <c>OS.HasFeature</c> calls for consistent mobile and AOT detection.
/// </summary>
public static class RuntimePlatform
{
    /// <summary>
    /// Returns <c>true</c> on Android and iOS platforms.
    /// </summary>
    public static bool IsMobileLike => OS.HasFeature("android") || OS.HasFeature("ios");

    /// <summary>
    /// Returns <c>true</c> when the runtime supports dynamic code generation (reflection emit, etc.).
    /// Typically <c>false</c> on mobile/AOT runtimes.
    /// </summary>
    public static bool SupportsDynamicCode => !IsMobileLike;
}
