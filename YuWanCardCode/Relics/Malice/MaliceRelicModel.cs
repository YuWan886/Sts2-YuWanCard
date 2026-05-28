using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Malice;
using YuWanCard.Modifiers;

namespace YuWanCard.Relics.Malice;

public abstract class MaliceRelicModel : YuWanRelicModel
{
    private static readonly YuWanCustomRelicRarity MaliceRarity = new(
        "YUWANCARD-MALICE",
        "relic_collection",
        "YUWANCARD-MALICE_CATEGORY.header",
        displayLocalizationTable: "relics",
        displayLocalizationKey: "YUWANCARD-MALICE_RARITY.label",
        visualRarity: RelicRarity.Uncommon,
        borderColor: new Color("8B0000"),
        sortOrder: 90);

    public override YuWanCustomRelicRarity? CustomRarity => MaliceRarity;

    public override bool IsAllowed(IRunState runState)
    {
        if (runState is not RunState run)
            return true;
        return MaliceModifier.GetMaliceModifier(run)?.EffectiveMaliceLevel >= 1;
    }

    public override bool IsAllowedInShops => false;

    protected MaliceRelicModel() { }

    protected MaliceRelicModel(bool autoAdd) : base(autoAdd) { }
}
