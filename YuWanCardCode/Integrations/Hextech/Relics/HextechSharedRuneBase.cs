using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public abstract class HextechSharedRuneBase : YuWanRelicModel
{
    public sealed override RelicRarity Rarity => RelicRarity.None;

    protected override string IconBasePath => $"res://YuWanCard/images/integrations/hextech/relics/{RelicId}";

    public sealed override string? CustomRarityLabelKey => "YUWANCARD-HEXTECH_RUNE_RARITY.label";

    public abstract HextechRuneRarity HextechRarity { get; }

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return true;
    }

    protected HextechSharedRuneBase() : base(true)
    {
    }
}
