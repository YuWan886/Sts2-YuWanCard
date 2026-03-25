using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Cards;

public abstract partial class YuWanCardModel : ConstructedCardModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://YuWanCard/images/card_portraits/{CardId}";
    protected virtual string FrameBasePath => $"res://YuWanCard/images/card_frames/{CardId}";

    public override string PortraitPath => $"{PortraitBasePath}.png";

    public override string? CustomPortraitPath => $"{PortraitBasePath}.png";

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

    protected YuWanCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true) 
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
    {
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
