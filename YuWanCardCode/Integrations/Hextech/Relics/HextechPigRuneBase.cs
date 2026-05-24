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
    public sealed override RelicRarity Rarity => RelicRarity.None;

    protected override string IconBasePath => $"res://YuWanCard/images/integrations/hextech/relics/{RelicId}";

    public abstract HextechRuneRarity HextechRarity { get; }

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return player.Character.Id == ModelDb.GetId<Characters.Pig>();
    }

    protected HextechPigRuneBase() : base(true)
    {
    }
}
