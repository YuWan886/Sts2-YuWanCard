using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;

namespace YuWanCard.Powers;

public abstract partial class YuWanPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/powers/{PowerId}.png";

    public override string? CustomPackedIconPath => IconBasePath;
    public override string? CustomBigIconPath => IconBasePath;

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
