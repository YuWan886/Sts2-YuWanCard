namespace YuWanCard.Core.Utils;

public static class AssetPathHelper
{
    // Asset paths must NOT derive from the assembly name: the content assembly is
    // packaged as "YuWanCard.Content" (a multi-version variant behind the YuWanCard
    // loader), while the Godot .pck keys and the manifest id are "YuWanCard".
    public const string ModId = "YuWanCard";

    public static string ModResPath => $"res://{ModId}";

    public static string GetModIdFromType(Type type) => ModId;

    public static string GetModResPathFromType(Type type) => $"res://{ModId}";

    public static string GetImagePath(Type contentType, string subPath)
    {
        return $"{GetModResPathFromType(contentType)}/images/{subPath}";
    }

    public static string GetScenePath(Type contentType, string subPath)
    {
        return $"{GetModResPathFromType(contentType)}/scenes/{subPath}";
    }

    public static string NormalizeId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return id;

        var colonIndex = id.IndexOf(':');
        if (colonIndex >= 0)
            id = id.Substring(colonIndex + 1);

        return id.ToLowerInvariant();
    }
}
