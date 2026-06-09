using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Runs;
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
        if (Owner == null || YUWANCARD_PendingRecycleGold <= 0)
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

        if (!TryGetRecycledGold(reward, out int recycledGold))
        {
            return;
        }

        recycleBin.QueueRecycledGold(recycledGold, reward.GetType().Name, LocalContext.NetId ?? 0);
    }

    internal static void QueueSyncedSkippedReward(RewardObtainedMessage message, ulong senderId)
    {
        Player? player = RunManager.Instance?.State?.GetPlayer(senderId);
        if (player == null)
        {
            MainFile.Logger.Warn($"RecycleBin: could not resolve reward sender {senderId} for synced skipped reward.");
            return;
        }

        var recycleBin = GetOwnedRelic(player);
        if (recycleBin == null)
        {
            return;
        }

        if (!TryGetRecycledGold(message, out int recycledGold, out string rewardType))
        {
            return;
        }

        recycleBin.QueueRecycledGold(recycledGold, rewardType, senderId);
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

    private void QueueRecycledGold(int recycledGold, string rewardType, ulong senderId)
    {
        if (recycledGold <= 0)
        {
            return;
        }

        YUWANCARD_PendingRecycleGold += recycledGold;
        InvokeDisplayAmountChanged();

        MainFile.Logger.Info(
            $"RecycleBin: queued {recycledGold} gold from skipped {rewardType} for player {Owner?.NetId} via sender {senderId}. Pending={YUWANCARD_PendingRecycleGold}.");
    }

    private static bool TryGetRecycledGold(Reward reward, out int recycledGold)
    {
        recycledGold = 0;

        decimal merchantValue = GetMerchantValue(reward);
        if (merchantValue <= 0)
        {
            return false;
        }

        recycledGold = GetRecycledGold(merchantValue);
        return recycledGold > 0;
    }

    private static bool TryGetRecycledGold(RewardObtainedMessage message, out int recycledGold, out string rewardType)
    {
        recycledGold = 0;
        rewardType = message.rewardType.ToString();

        decimal merchantValue = message.rewardType switch
        {
            RewardType.Card when message.cardModel != null => GetCardMerchantValue(message.cardModel),
            RewardType.Potion when message.potionModel != null => GetPotionMerchantValue(message.potionModel),
            RewardType.Relic when message.relicModel != null => GetRelicMerchantValue(message.relicModel),
            _ => 0m
        };

        if (merchantValue <= 0)
        {
            return false;
        }

        recycledGold = GetRecycledGold(merchantValue);
        return recycledGold > 0;
    }

    private static int GetRecycledGold(decimal merchantValue)
    {
        return (int)Math.Floor(merchantValue * RecoveryRate);
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
