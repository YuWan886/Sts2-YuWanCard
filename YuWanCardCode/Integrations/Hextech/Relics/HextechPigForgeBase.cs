using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;
using YuWanCard.Integrations.Hextech.RelicPools;

namespace YuWanCard.Hextech.Relics;

[Pool(typeof(HextechPigRunePool))]
public abstract class HextechPigForgeBase : YuWanRelicModel
{
    private const string HextechForgeIconBasePath = "res://HextechRunes/images/relics";

    public sealed override RelicRarity Rarity => RelicRarity.None;

    protected override string IconBasePath => $"{HextechForgeIconBasePath}/{GetForgeIconStem()}";

    public sealed override string? CustomRarityLabelKey => "YUWANCARD-HEXTECH_RUNE_RARITY.label";

    public abstract HextechForgeRarity HextechRarity { get; }

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return player.Character.Id == ModelDb.GetId<Characters.Pig>();
    }

    protected HextechPigForgeBase() : base(true)
    {
    }

    private string GetForgeIconStem()
    {
        return HextechRarity switch
        {
            HextechForgeRarity.Silver => "silverForge",
            HextechForgeRarity.Gold => "goldForge",
            HextechForgeRarity.Prismatic => "prismaticForge",
            _ => "silverForge"
        };
    }
}
