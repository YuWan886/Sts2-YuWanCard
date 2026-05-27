using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Abstracts;

public abstract partial class YuWanRelicModel : RelicModel, IYuWanContent
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private static readonly string DefaultIconPath = "res://YuWanCard/images/relics/pig_carrot.png";

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    
    protected string ModResPath => AssetPathHelper.GetModResPathFromType(GetType());
    
    protected virtual string IconBasePath => $"{ModResPath}/images/relics/{RelicId}";

    private string GetIconPath(string path) => ResourceLoader.Exists(path) ? path : DefaultIconPath;

    protected override string BigIconPath => GetIconPath($"{IconBasePath}.png");
    public override string PackedIconPath => GetIconPath($"{IconBasePath}.png");
    protected override string PackedIconOutlinePath => GetIconPath($"{IconBasePath}.png");

    protected YuWanRelicModel() : base()
    {
    }

    protected YuWanRelicModel(bool autoAdd) : this()
    {
        if (autoAdd) ContentRegistry.AddModel(GetType());
    }

    public virtual YuWanCustomRelicRarity? CustomRarity => null;

    /// <summary>
    /// Optional custom localization key for the rarity label shown in the relic inspect screen.
    /// When non-null, replaces the standard "RELIC_RARITY.{Rarity}" lookup in the "gameplay_ui" table.
    /// </summary>
    public virtual string? CustomRarityLabelKey => null;

    public virtual RelicModel? GetUpgradeReplacement() => null;

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
