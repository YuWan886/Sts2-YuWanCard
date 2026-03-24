using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Relics;

public abstract partial class YuWanRelicModel : CustomRelicModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string IconBasePath => $"res://YuWanCard/images/relics/{RelicId}";
    public override string PackedIconPath => $"{IconBasePath}.png";
    public override string BigIconPath => $"{IconBasePath}.png";
    public override string PackedIconOutlinePath => $"{IconBasePath}.png";

    protected YuWanRelicModel() : base()
    {
    }

    protected YuWanRelicModel(bool autoAdd) : base(autoAdd)
    {
    }
    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
