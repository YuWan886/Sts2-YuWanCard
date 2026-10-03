using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Relics;
using YuWanCard.Core.Abstracts;
using YuWanCard.Hextech;
using YuWanCard.Integrations.Hextech.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(HextechPigRunePool))]
public abstract class HextechPigRuneBase : YuWanRelicModel
{
    // HextechRunes only grants external player runes whose Rarity is Starter: vanilla has
    // several rarity-driven relic paths beyond the pools Hextech filters, and Starter is the
    // only tier none of them roll. Hextech renders its own runes' custom rarity on top.
    public sealed override RelicRarity Rarity => RelicRarity.Starter;

    protected override string IconBasePath => $"res://YuWanCard/images/integrations/hextech/relics/{RelicId}";

    public sealed override string? CustomRarityLabelKey => "YUWANCARD-HEXTECH_RUNE_RARITY.label";

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return player.Character.Id == ModelDb.GetId<Characters.Pig>();
    }

    protected HextechPigRuneBase() : base(true)
    {
    }
}
