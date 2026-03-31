using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Powers;

public abstract partial class YuWanPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/powers/{PowerId}.png";

    public override string? CustomPackedIconPath => IconBasePath;
    public override string? CustomBigIconPath => IconBasePath;

    /// <summary>
    /// 根据类型名称自动生成能力 ID（格式：camel_case_name）
    /// </summary>
    public static string GeneratePowerId<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return CamelCaseRegex.Replace(typeName, "$1_$2").ToLowerInvariant();
    }

    /// <summary>
    /// 根据类型名称自动生成图标路径
    /// </summary>
    public static string GenerateIconPath<T>() where T : class
    {
        return $"res://YuWanCard/images/powers/{GeneratePowerId<T>()}.png";
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
