using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public abstract class HextechSharedRuneBase : YuWanRelicModel
{
    // See HextechPigRuneBase: HextechRunes requires Starter rarity for external player runes.
    public sealed override RelicRarity Rarity => RelicRarity.Starter;

    protected override string IconBasePath => $"res://YuWanCard/images/integrations/hextech/relics/{RelicId}";

    public sealed override string? CustomRarityLabelKey => "YUWANCARD-HEXTECH_RUNE_RARITY.label";

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return true;
    }

    protected HextechSharedRuneBase() : base(true)
    {
    }
}
