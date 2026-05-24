using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using YuWanCard.Core.Abstracts;
using YuWanCard.Integrations.Hextech.RelicPools;

namespace YuWanCard.Hextech.Relics;

[Pool(typeof(HextechPigRunePool))]
public abstract class HextechPigForgeBase : YuWanRelicModel
{
    public sealed override RelicRarity Rarity => RelicRarity.None;

    protected override string IconBasePath => $"res://YuWanCard/images/integrations/hextech/relics/{RelicId}";

    public abstract HextechForgeRarity HextechRarity { get; }

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return player.Character.Id == MegaCrit.Sts2.Core.Models.ModelDb.GetId<Characters.Pig>();
    }

    protected HextechPigForgeBase() : base(true)
    {
    }
}
