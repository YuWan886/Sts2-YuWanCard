using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class InvestorJoker : BalatroJokerRelicModel
{
    private const int RefundRatioPercent = 20;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        await base.AfterItemPurchased(player, itemPurchased, goldSpent);

        if (Owner == null || player != Owner)
        {
            return;
        }

        int multiplier = EffectiveCount();
        if (multiplier <= 0)
        {
            return;
        }

        int refund = (int)Math.Floor(goldSpent * (RefundRatioPercent / 100m) * multiplier);
        if (refund > 0)
        {
            await PlayerCmd.GainGold(refund, player);
        }
    }

}
