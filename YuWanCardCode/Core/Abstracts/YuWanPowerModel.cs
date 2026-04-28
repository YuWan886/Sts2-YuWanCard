using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Abstracts;

public abstract partial class YuWanPowerModel : PowerModel, IYuWanContent
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private const string DefaultIconPath = "res://YuWanCard/images/powers/pig_doubt_power.png";

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/powers/{PowerId}.png";

    public virtual string? CustomPackedIconPath => GetIconPath();
    public virtual string? CustomBigIconPath => GetIconPath();

    private string? GetIconPath()
    {
        var iconPath = IconBasePath;
        if (ResourceLoader.Exists(iconPath))
            return iconPath;
        return DefaultIconPath;
    }

    public static string GeneratePowerId<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return CamelCaseRegex.Replace(typeName, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateIconPath<T>() where T : class
    {
        return $"res://YuWanCard/images/powers/{GeneratePowerId<T>()}.png";
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
