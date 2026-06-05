using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Modifiers;

namespace YuWanCard.Relics.Balatro;

public abstract class BalatroRelicModel : YuWanRelicModel
{
    private static readonly YuWanCustomRelicRarity BalatroRarity = new(
        "YUWANCARD-BALATRO",
        "relic_collection",
        "YUWANCARD-BALATRO_CATEGORY.header",
        displayLocalizationTable: "relics",
        displayLocalizationKey: "YUWANCARD-BALATRO_RARITY.label",
        visualRarity: RelicRarity.Uncommon,
        borderColor: new Color("E8D1A0"),
        sortOrder: 85);

    public override YuWanCustomRelicRarity? CustomRarity => BalatroRarity;

    protected BalatroRelicModel() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState)
    {
        return BalatroModifier.IsActive(runState);
    }
}
