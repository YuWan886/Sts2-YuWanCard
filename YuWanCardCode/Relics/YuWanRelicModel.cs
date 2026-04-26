using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;

namespace YuWanCard.Relics;

public abstract partial class YuWanRelicModel : CustomRelicModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private static readonly string DefaultIconPath = "res://YuWanCard/images/relics/pig_carrot.png";

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string IconBasePath => $"res://YuWanCard/images/relics/{RelicId}";
    
    private string GetIconPath(string path) => ResourceLoader.Exists(path) ? path : DefaultIconPath;
    
    public override string PackedIconPath => GetIconPath($"{IconBasePath}.png");
    protected override string BigIconPath => GetIconPath($"{IconBasePath}.png");
    protected override string PackedIconOutlinePath => GetIconPath($"{IconBasePath}.png");

    protected YuWanRelicModel() : base()
    {
    }

    protected YuWanRelicModel(bool autoAdd) : base(autoAdd)
    {
    }
    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
