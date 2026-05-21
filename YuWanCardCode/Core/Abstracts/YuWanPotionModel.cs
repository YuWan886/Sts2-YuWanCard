using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Abstracts;

public abstract partial class YuWanPotionModel : PotionModel, IYuWanContent
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private const string DefaultImagePath = "res://YuWanCard/images/relics/pig_carrot.png";

    protected virtual string PotionId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected string ModResPath => AssetPathHelper.GetModResPathFromType(GetType());

    protected virtual string ImageBasePath => $"{ModResPath}/images/potions/{PotionId}";

    public virtual string? CustomPackedImagePath => GetImagePath($"{ImageBasePath}.png");

    public virtual string? CustomPackedOutlinePath => GetOutlinePath();

    protected YuWanPotionModel()
    {
        ContentRegistry.AddModel(GetType());
    }

    private static string GeneratePotionId(Type type) =>
        CamelCaseRegex.Replace(type.Name, "$1_$2").ToLowerInvariant();

    private static string GenerateImageBasePath(Type type) =>
        $"{AssetPathHelper.GetModResPathFromType(type)}/images/potions/{GeneratePotionId(type)}";

    private static string GetImagePath(string path) => ResourceLoader.Exists(path) ? path : DefaultImagePath;

    private string? GetOutlinePath()
    {
        var outlinePath = $"{ImageBasePath}_outline.png";
        if (ResourceLoader.Exists(outlinePath))
        {
            return outlinePath;
        }

        return CustomPackedImagePath;
    }

    public static string GeneratePotionId<T>() where T : class => GeneratePotionId(typeof(T));

    public static string GenerateImagePath<T>() where T : class =>
        $"{GenerateImageBasePath(typeof(T))}.png";

    public static string GenerateOutlinePath<T>() where T : class =>
        $"{GenerateImageBasePath(typeof(T))}_outline.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
