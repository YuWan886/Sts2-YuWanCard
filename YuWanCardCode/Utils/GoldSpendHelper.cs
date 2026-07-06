using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Utils;

public static class GoldSpendHelper
{
    public static bool CanAfford(Player? player, int goldCost)
    {
        return player != null && goldCost >= 0 && player.Gold >= goldCost;
    }

    public static async Task<bool> TrySpend(Player? player, int goldCost, string source)
    {
        if (!CanAfford(player, goldCost))
        {
            MainFile.Logger.Warn($"{source}: Not enough gold ({player?.Gold ?? 0} < {goldCost})");
            return false;
        }

        if (goldCost <= 0)
        {
            return true;
        }

        await PlayerCmd.LoseGold(goldCost, player!, GoldLossType.Spent);
        return true;
    }
}
