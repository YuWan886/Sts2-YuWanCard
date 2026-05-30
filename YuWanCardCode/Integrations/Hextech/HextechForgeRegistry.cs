using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Hextech.Relics;

namespace YuWanCard.Hextech;

public static class HextechForgeRegistry
{
    private static readonly IReadOnlyList<Type> SilverForges =
    [
        typeof(PigletCollarForge)
    ];

    public static IReadOnlyList<Type> GetAllForges() => SilverForges;

    public static IReadOnlyList<Type> GetForgesByRarity(HextechForgeRarity rarity)
    {
        return rarity switch
        {
            HextechForgeRarity.Silver => SilverForges,
            _ => Array.Empty<Type>()
        };
    }

    public static bool IsPigForge(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return GetAllForges().Any(type => ModelDb.GetId(type) == id);
    }

    public static bool TryGetRarity(RelicModel? relic, out HextechForgeRarity rarity)
    {
        rarity = default;
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        foreach (HextechForgeRarity value in Enum.GetValues<HextechForgeRarity>())
        {
            if (GetForgesByRarity(value).Any(type => ModelDb.GetId(type) == id))
            {
                rarity = value;
                return true;
            }
        }

        return false;
    }

    public static bool IsAvailableForPlayer(RelicModel relic, Player player)
    {
        return player.Character.Id == ModelDb.GetId<Characters.Pig>();
    }
}
