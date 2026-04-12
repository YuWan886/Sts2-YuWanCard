using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Cards;

public abstract partial class YuWanCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true) : ConstructedCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    private const string TestPortraitPath = "res://YuWanCard/images/card_portraits/you_are_pig.png";

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://YuWanCard/images/card_portraits/{CardId}";
    protected virtual string FrameBasePath => $"res://YuWanCard/images/card_frames/{CardId}";

    public override string PortraitPath => GetPortraitPath();

    public override string? CustomPortraitPath => GetPortraitPath();

    private string GetPortraitPath()
    {
        string portraitPath = $"{PortraitBasePath}.png";
        if (ResourceLoader.Exists(portraitPath))
        {
            return portraitPath;
        }
        return TestPortraitPath;
    }

    public override Texture2D? CustomFrame
    {
        get
        {
            string framePath = $"{FrameBasePath}.png";
            if (ResourceLoader.Exists(framePath))
            {
                return ResourceLoader.Load<Texture2D>(framePath);
            }
            return null;
        }
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
