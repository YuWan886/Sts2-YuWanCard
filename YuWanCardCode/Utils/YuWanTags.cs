using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Utils;

public static class YuWanTags
{
    public static readonly CardTag FoodPig;

    static YuWanTags()
    {
        var registry = ModCardTagRegistry.For("YUWANCARD");
        FoodPig = registry.RegisterOwned("FOOD_PIG");
    }
}
