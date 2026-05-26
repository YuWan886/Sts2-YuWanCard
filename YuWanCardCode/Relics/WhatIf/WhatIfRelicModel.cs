using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

public abstract class WhatIfRelicModel : YuWanRelicModel
{
    private static readonly YuWanCustomRelicRarity WhatIfRarity =
        new(
            "YUWANCARD-WHAT_IF",
            "relic_collection",
            "YUWANCARD-WHAT_IF_CATEGORY.header",
            displayLocalizationTable: "relics",
            displayLocalizationKey: "YUWANCARD-WHAT_IF_RARITY.label",
            visualRarity: RelicRarity.Event,
            borderColor: new Color("741ADB"),
            sortOrder: 100);

    public sealed override RelicRarity Rarity => RelicRarity.None;

    public override YuWanCustomRelicRarity? CustomRarity => WhatIfRarity;

    public override int MerchantCost => 999999999;

    public override bool IsAllowedInShops => false;

    protected WhatIfRelicModel()
    {
    }

    protected WhatIfRelicModel(bool autoAdd) : base(autoAdd)
    {
    }
}
