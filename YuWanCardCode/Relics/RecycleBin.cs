using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
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
    private const int InvalidMerchantCostSentinel = 999999999;

    static RecycleBin()
    {
        SavedPropertyRegistration.RegisterType(typeof(RecycleBin));
    }

    // Runtime-authoritative pending gold. This counter only accrues/resets on the OWNING client:
    // rewards are skipped/selected through that client's local rewards UI
    // (RecycleBinRewardPatch -> NRewardsScreen.AfterOverlayClosed), NOT a synchronized GameAction,
    // so the value is inherently per-client. All runtime logic reads/writes this field directly.
    private int _pendingRecycleGold;

    // Save bridge for single-player only. relic.ToSerializable() -> SavedProperties.From feeds BOTH
    // the disk save AND the multiplayer state checksum (NetFullCombatState), so a per-client value
    // here would diverge and trip StateDivergence (host disconnects the client). In real multiplayer
    // the getter reports the type default, so SaveIfNotTypeDefault omits it entirely -> never in the
    // checksum. The trade-off: pending gold is not persisted across a save/quit during an MP session.
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    private int YUWANCARD_PendingRecycleGold
    {
        get => RunManager.Instance?.IsSingleplayerOrFakeMultiplayer == true ? _pendingRecycleGold : 0;
        set => _pendingRecycleGold = value;
    }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => _pendingRecycleGold > 0;

    public override int DisplayAmount => _pendingRecycleGold;

    public RecycleBin() : base(true)
    {
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // Only pay out at a NON-combat room. RewardSynchronizer buffers the gold-obtained message
        // on remote clients until combat ends (HandleRewardObtainedMessage), while the owner applies
        // it immediately on room-enter. Granting at a combat boundary therefore leaves owner.Gold
        // ahead of the other clients for the whole fight, and the mid-combat checksum ("After player
        // turn start") catches that transient -> StateDivergence. Deferring to the next non-combat
        // room means both the local grant and the synced grant land outside combat and stay in step,
        // mirroring vanilla MawBank/GoldReward, which only move gold outside combat.
        if (Owner == null || _pendingRecycleGold <= 0 || !LocalContext.IsMe(Owner) || room is CombatRoom)
        {
            return;
        }

        int pendingGold = _pendingRecycleGold;
        _pendingRecycleGold = 0;
        InvokeDisplayAmountChanged();

        Flash();
        await PlayerCmd.GainGold(pendingGold, Owner);
        RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedGold(pendingGold);
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

    internal static void QueueSkippedCards(Player player, IEnumerable<CardModel> cards, string source)
    {
        if (!LocalContext.IsMe(player))
        {
            return;
        }

        List<CardModel> skippedCards = cards.ToList();
        if (skippedCards.Count == 0)
        {
            return;
        }

        var recycleBin = GetOwnedRelic(player);
        if (recycleBin == null)
        {
            return;
        }

        if (!TryGetRecycledGold(skippedCards, out int recycledGold))
        {
            return;
        }

        recycleBin.QueueRecycledGold(
            recycledGold,
            $"{nameof(CardReward)}[{skippedCards.Count}]/{source}",
            LocalContext.NetId ?? 0);
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

        _pendingRecycleGold += recycledGold;
        InvokeDisplayAmountChanged();

        MainFile.Logger.Info(
            $"RecycleBin: queued {recycledGold} gold from skipped {rewardType} for player {Owner?.NetId} via sender {senderId}. Pending={_pendingRecycleGold}.");
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

    private static bool TryGetRecycledGold(IEnumerable<CardModel> skippedCards, out int recycledGold)
    {
        recycledGold = 0;
        decimal merchantValue = skippedCards.Sum(GetCardMerchantValue);

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
        int merchantCost = relic.MerchantCost;
        if (!relic.IsAllowedInShops || merchantCost <= 0 || merchantCost >= InvalidMerchantCostSentinel)
        {
            return 0m;
        }

        return merchantCost;
    }
}
