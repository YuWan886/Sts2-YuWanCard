using MegaCrit.Sts2.Core.Entities.Cards;
using YuWanCard.Core.Patches.Content;

namespace YuWanCard.Balatro;

public static class BalatroCardKeywords
{
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Foil;

    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Holographic;

    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Polychrome;

    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Negative;
}
