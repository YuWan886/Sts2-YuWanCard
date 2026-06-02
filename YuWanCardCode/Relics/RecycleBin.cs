using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class RecycleBin : YuWanRelicModel
{
    private const decimal RecoveryRate = 0.3m;

    static RecycleBin()
    {
        SavedPropertyRegistration.RegisterType(typeof(RecycleBin));
    }

    [SavedProperty]
    private int YUWANCARD_PendingRecycleGold { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => YUWANCARD_PendingRecycleGold > 0;

    public override int DisplayAmount => YUWANCARD_PendingRecycleGold;

    public RecycleBin() : base(true)
    {
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null || !LocalContext.IsMe(Owner) || YUWANCARD_PendingRecycleGold <= 0)
        {
            return;
        }

        int pendingGold = YUWANCARD_PendingRecycleGold;
        YUWANCARD_PendingRecycleGold = 0;
        InvokeDisplayAmountChanged();

        Flash();
        await PlayerCmd.GainGold(pendingGold, Owner);
    }

    internal static void QueueSkippedReward(Reward reward)
    {
        if (!LocalContext.IsMe(reward.Player))
        {
            return;
        }

        var recycleBin = GetOwnedRelic(reward.Player);
        if (recycleBin == null)
        {
            return;
        }

        decimal merchantValue = GetMerchantValue(reward);
        if (merchantValue <= 0)
        {
            return;
        }

        recycleBin.QueueMerchantValue(merchantValue, reward.GetType().Name);
    }

    private static RecycleBin? GetOwnedRelic(Player? player)
    {
        if (player == null)
        {
            return null;
        }

        foreach (var relic in player.Relics)
        {
            if (relic is RecycleBin recycleBin)
            {
                return recycleBin;
            }
        }

        return null;
    }

    private void QueueMerchantValue(decimal merchantValue, string rewardType)
    {
        int recycledGold = (int)Math.Floor(merchantValue * RecoveryRate);
        if (recycledGold <= 0)
        {
            return;
        }

        YUWANCARD_PendingRecycleGold += recycledGold;
        InvokeDisplayAmountChanged();

        MainFile.Logger.Info(
            $"RecycleBin: queued {recycledGold} gold from skipped {rewardType}. Pending={YUWANCARD_PendingRecycleGold}.");
    }

    private static decimal GetMerchantValue(Reward reward)
    {
        return reward switch
        {
            CardReward cardReward => cardReward.Cards.Sum(GetCardMerchantValue),
            PotionReward potionReward when potionReward.Potion != null => GetPotionMerchantValue(potionReward.Potion),
            RelicReward relicReward when YuWanReflectionHelper.GetPrivateField<RelicModel>(relicReward, "_relic") is { } relic
                => GetRelicMerchantValue(relic),
            _ => 0m
        };
    }

    private static decimal GetCardMerchantValue(CardModel card)
    {
        decimal value = card.Rarity switch
        {
            CardRarity.Rare => 150m,
            CardRarity.Uncommon => 75m,
            _ => 50m
        };

        if (card.Pool is ColorlessCardPool)
        {
            value *= 1.15m;
        }

        return Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static decimal GetPotionMerchantValue(PotionModel potion)
    {
        return potion.Rarity switch
        {
            PotionRarity.Rare => 100m,
            PotionRarity.Uncommon => 75m,
            _ => 50m
        };
    }

    private static decimal GetRelicMerchantValue(RelicModel relic)
    {
        return relic.MerchantCost;
    }
}
