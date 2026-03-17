using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class RingOfSevenCurses : YuWanRelicModel
{
    private decimal _pendingGoldReduction;
    private bool _isApplyingReduction;
    [SavedProperty]
    private bool PotionSlotsAdded { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public RingOfSevenCurses() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (!PotionSlotsAdded)
        {
            Owner?.AddToMaxPotionCount(1);
            PotionSlotsAdded = true;
        }
    }

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        if (_isApplyingReduction)
        {
            return true;
        }
        if (player != Owner)
        {
            return true;
        }
        _pendingGoldReduction = Math.Floor(amount * 0.5m);
        return true;
    }

    public override async Task AfterGoldGained(Player player)
    {
        if (player == Owner && !_isApplyingReduction && _pendingGoldReduction > 0m)
        {
            decimal reduction = _pendingGoldReduction;
            _pendingGoldReduction = 0m;
            _isApplyingReduction = true;
            await PlayerCmd.LoseGold(reduction, player);
            _isApplyingReduction = false;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        var availableCurses = ModelDb.CardPool<CurseCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .ToHashSet();
        if (availableCurses.Count == 0)
        {
            return;
        }
        Flash();
        CardModel? curseCard = Owner.RunState.Rng.Niche.NextItem(availableCurses);
        if (curseCard == null || Owner.Creature.CombatState == null)
        {
            return;
        }
        CardModel card = Owner.Creature.CombatState.CreateCard(curseCard, Owner);
        var results = await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, addedByPlayer: true);
        if (results.Count == 0 || !results[0].success)
        {
            MainFile.Logger.Warn($"RingOfSevenCurses: Failed to add curse card to hand");
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != null && target.Player == Owner)
        {
            return 1.5m;
        }
        if (dealer != null && dealer.Player == Owner)
        {
            if (target != null && target.CombatState?.Encounter?.RoomType == RoomType.Boss)
            {
                return 1.5m;
            }
            return 0.75m;
        }
        return 1m;
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target.Player == Owner)
        {
            return 0.8m;
        }
        return 1m;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player == Owner)
        {
            return amount + 1;
        }
        return amount;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player == Owner)
        {
            return count + 1;
        }
        return count;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }
        if (room == null)
        {
            return false;
        }

        if (room.RoomType == RoomType.Monster || room.RoomType == RoomType.Boss)
        {
            rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
        }

        if (room.RoomType == RoomType.Monster && Owner.RunState.Rng.Niche.NextFloat() < 0.5f)
        {
            rewards.Add(new RelicReward(player));
        }
        return true;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (creature.Player == Owner || creature.PetOwner == Owner)
        {
            return amount * 0.5m;
        }
        return amount;
    }

    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText)
    {
        if (player != Owner)
        {
            return currentExtraText;
        }

        var list = new List<LocString>(currentExtraText);
        var extraText = new LocString("relics", "YUWANCARD-RING_OF_SEVEN_CURSES.additionalRestSiteHealText");
        decimal baseHeal = (decimal)player.Creature.MaxHp * 0.3m;
        decimal actualHeal = baseHeal * 0.5m;
        int actualHealInt = (int)actualHeal;
        extraText.Add("ActualHeal", actualHealInt.ToString());
        list.Add(extraText);
        return list;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (room == null || room.RoomType != RoomType.Boss)
        {
            return;
        }
        if (Owner == null)
        {
            return;
        }
        int maxHpLoss = (int)Math.Floor(Owner.Creature.MaxHp * 0.25m);
        if (maxHpLoss > 0)
        {
            Flash();
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, maxHpLoss, isFromCard: false);
        }
    }

}
