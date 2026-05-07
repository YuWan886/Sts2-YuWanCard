using System.Reflection;

namespace YuWanCard.Core.Utils;

public static class AssetPathHelper
{
    private static string? _modId;
    private static string? _modResPath;

    public static string ModId
    {
        get
        {
            if (_modId != null)
                return _modId;

            var assembly = Assembly.GetExecutingAssembly();
            _modId = assembly.GetName().Name ?? "YuWanCard";
            return _modId;
        }
    }

    public static string ModResPath
    {
        get
        {
            if (_modResPath != null)
                return _modResPath;

            _modResPath = $"res://{ModId}";
            return _modResPath;
        }
    }

    public static string GetModIdFromType(Type type)
    {
        var assembly = type.Assembly;
        var name = assembly.GetName().Name;
        if (string.IsNullOrEmpty(name))
            return ModId;

        return name;
    }

    public static string GetModResPathFromType(Type type)
    {
        var modId = GetModIdFromType(type);
        return $"res://{modId}";
    }

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
