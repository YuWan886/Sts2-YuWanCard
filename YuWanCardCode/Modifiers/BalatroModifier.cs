using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Balatro;
using YuWanCard.Cards;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Persistence;
using YuWanCard.Powers;
using YuWanCard.Relics;
using YuWanCard.Relics.Balatro;
using YuWanCard.Utils;

namespace YuWanCard.Modifiers;

public sealed class BalatroModifier : YuWanModifierModel
{
    // Interest
    private const decimal InterestRate = 0.1m;
    private const int BaseInterestCap = 10;

    // Combo
    private const float ComboMultiplierPerPoint = 0.1f;
    private const float MaxCombo = 30f;
    private const float RetainedComboThreshold = 20f;
    private const float DefaultRetainRatio = 0.1f;
    private const float SteelJokerRetainRatio = 0.2f;
    private const float RetainedComboScale = 20f;
    private const float LegendBonusPerCard = 0.2f;
    private const int NoCardType = -1;
    private const string ActiveTurnPlayerNetIdStateName = "YUWANCARD_BalatroActiveTurnPlayerNetId";

    // Mod Station
    private const int ModStationRefreshCost = 25;
    private const int ModStationFoilCost = 75;
    private const int ModStationHolographicCost = 75;
    private const int ModStationPolychromeCost = 150;
    private const int ModStationNegativeCost = 250;

    #region Saved State

    public override bool AllowedInCustomRun => true;

    private static readonly SavedAttachedState<Player, int> RetainedComboScaledState =
        new("YUWANCARD_BalatroRetainedComboScaled", () => 0);

    private static readonly SavedAttachedState<Player, int> LastInterestFloorState =
        new("YUWANCARD_BalatroLastInterestFloor", () => 0);

    private static readonly SavedAttachedState<Player, SerializableCard> CurrentTurnFirstCardState =
        new("YUWANCARD_BalatroCurrentTurnFirstCard", _ => null!);

    private static readonly SavedAttachedState<Player, SerializableCard> PreviousTurnFirstCardState =
        new("YUWANCARD_BalatroPreviousTurnFirstCard", _ => null!);

    private static readonly SavedAttachedState<Player, int> ModifierTokensState =
        new("YUWANCARD_BalatroModifierTokens", () => 0);

    private static readonly SavedAttachedState<Player, int> ModStationOffer1State =
        new("YUWANCARD_BalatroModStationOffer1", () => 0);

    private static readonly SavedAttachedState<Player, int> ModStationOffer2State =
        new("YUWANCARD_BalatroModStationOffer2", () => 0);

    private static readonly SavedAttachedState<Player, int> ModStationFloorState =
        new("YUWANCARD_BalatroModStationFloor", () => 0);

    private static readonly SavedAttachedState<Player, int> ComboCounterScaledState =
        new("YUWANCARD_BalatroComboCounterScaled", () => 0);

    private static readonly SavedAttachedState<Player, int> CardsPlayedThisTurnState =
        new("YUWANCARD_BalatroCardsPlayedThisTurn", () => 0);

    private static readonly SavedAttachedState<Player, int> AttackCardsThisTurnState =
        new("YUWANCARD_BalatroAttackCardsThisTurn", () => 0);

    private static readonly SavedAttachedState<Player, int> SkillCardsThisTurnState =
        new("YUWANCARD_BalatroSkillCardsThisTurn", () => 0);

    private static readonly SavedAttachedState<Player, int> LastCardTypeThisTurnState =
        new("YUWANCARD_BalatroLastCardTypeThisTurn", () => NoCardType);

    private static readonly SavedAttachedState<RunState, string> ActiveTurnPlayerNetIdState =
        new(ActiveTurnPlayerNetIdStateName, () => string.Empty);

    #endregion

    #region Runtime State

    public override bool AllowedInDailyRun => false;

    public override IEnumerable<IHoverTip> HoverTips =>
    [
        new HoverTip(
            new LocString("modifiers", "YUWANCARD-BALATRO.title"),
            new LocString("modifiers", "YUWANCARD-BALATRO.description"))
    ];

    #endregion

    #region Lifecycle & Room Hooks

    protected override void AfterRunCreated(RunState runState)
    {
        base.AfterRunCreated(runState);
    }

    public override async Task BeforeCombatStart()
    {
        ResetCombatState();
        await base.BeforeCombatStart();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await base.AfterRoomEntered(room);

        foreach (Player player in RunState.Players)
        {
            await ApplyInterestForRoom(player);
        }
    }

    #endregion

    #region Turn & Card Hooks

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        SetActiveTurnPlayer(player);

        float retainedCombo = GetRetainedCombo(player);
        if (retainedCombo > 0f)
        {
            SetComboCounter(player, Math.Min(MaxCombo, retainedCombo));
            SetRetainedCombo(player, 0f);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        Player? player = GetActiveTurnPlayer();
        if (player == null || side != player.Creature.Side)
        {
            return;
        }

        float comboCounter = GetComboCounter(player);
        float retainRatio = 0f;
        if (comboCounter >= RetainedComboThreshold)
        {
            retainRatio = DefaultRetainRatio;
            await PowerCmd.Apply<InertiaPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
        }

        if (player.GetRelic<SteelJoker>() != null)
        {
            retainRatio = Math.Max(retainRatio, SteelJokerRetainRatio);
        }

        SetRetainedCombo(player, retainRatio > 0f
            ? MathF.Min(MaxCombo, comboCounter * retainRatio)
            : 0f);
        SetPreviousTurnFirstCard(player, GetCurrentTurnFirstCard(player));
        ResetTurnState(player);
        SetActiveTurnPlayer(null);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Player? player = cardPlay.Card.Owner;
        if (player == null)
        {
            return Task.CompletedTask;
        }

        CardModel card = cardPlay.Card;
        if (GetCurrentTurnFirstCard(player) == null)
        {
            SetCurrentTurnFirstCard(player, card.ToSerializable());
        }

        float comboGain = CalculateComboGain(player, card);
        if (comboGain <= 0f)
        {
            return Task.CompletedTask;
        }

        AddCombo(player, comboGain);
        CardsPlayedThisTurnState[player] = GetCardsPlayedThisTurn(player) + 1;

        if (card.Type == CardType.Attack)
        {
            AttackCardsThisTurnState[player] = GetAttackCardsThisTurn(player) + 1;
        }
        else if (card.Type == CardType.Skill)
        {
            SkillCardsThisTurnState[player] = GetSkillCardsThisTurn(player) + 1;
        }

        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner == null)
        {
            return playCount;
        }

        int extra = 0;
        if (BalatroCardEditionHelper.HasEdition(card))
        {
            extra += BalatroCardEditionHelper.GetPlayCountBonus(card);
        }

        return playCount + extra;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return 0m;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        Player? player = dealer?.Player;
        if (player == null || dealer != player.Creature || cardSource?.Type != CardType.Attack)
        {
            return 1m;
        }

        return (decimal)GetComboMultiplier(player);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (BalatroCardEditionHelper.GetEdition(card) != BalatroCardEdition.Holographic)
        {
            return false;
        }

        modifiedCost = Math.Max(0m, originalCost - 1m);
        return true;
    }

    #endregion

    #region Shop, Rewards & Mod Station

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
        {
            return options;
        }

        if (options.Source is not CardCreationSource.Encounter and not CardCreationSource.Shop)
        {
            return options;
        }

        List<CardModel> pool = options.GetPossibleCards(player).ToList();
        foreach (CardModel card in GetBalatroRewardCards(player))
        {
            if (pool.All(existing => existing.Id != card.Id))
            {
                pool.Add(card);
            }
        }

        if (pool.Count == 0)
        {
            return options;
        }

        bool singleRarity = pool.Select(card => card.Rarity).Distinct().Count() == 1;
        return options.WithCustomPool(pool, singleRarity ? CardRarityOddsType.Uniform : null);
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (room == null || room.RoomType is not RoomType.Elite and not RoomType.Boss)
        {
            return false;
        }

        bool modified = false;
        if (!HasBalatroRelicReward(rewards))
        {
            float jokerChance = room.RoomType == RoomType.Boss ? 0.5f : 0.10f;
            if (DeterministicRandomUtils.RollProbability(player.PlayerRng.Rewards, jokerChance))
            {
                List<RelicModel> available = GetAvailableJokers(player);
                RelicModel? reward = DeterministicRandomUtils.PickDeterministicRelic(available, player.PlayerRng.Rewards);
                if (reward != null)
                {
                    rewards.Add(new RelicReward(reward.ToMutable(), player));
                    modified = true;
                }
            }
        }

        if (!HasBalatroRelicReward(rewards))
        {
            float tokenChance = room.RoomType == RoomType.Boss ? 0.20f : 0.05f;
            if (DeterministicRandomUtils.RollProbability(player.PlayerRng.Rewards, tokenChance))
            {
                rewards.Add(new RelicReward(ModelDb.Relic<ModifierToken>().ToMutable(), player));
                modified = true;
            }
        }

        return modified;
    }

    public override decimal ModifyGoldGained(Player player, decimal amount)
    {
        return amount;
    }

    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        await Task.CompletedTask;
    }

    #endregion

    #region Joker Management

    public static BalatroModifier? GetInstance(RunState state)
    {
        return state.Modifiers.OfType<BalatroModifier>().FirstOrDefault();
    }

    public static bool IsActive(IRunState runState)
    {
        return runState is RunState state && GetInstance(state) != null;
    }

    public string GetComboDisplayText(Player? player)
    {
        float combo = GetComboCounter(player);
        float multiplier = GetComboMultiplier(player);
        return $"COMBO {combo:0.#}  MULT x{multiplier:0.0}";
    }

    public float GetComboCounter(Player? player)
    {
        return GetScaledState(ComboCounterScaledState, player);
    }

    public float GetComboMultiplier(Player? player)
    {
        return 1f + GetComboCounter(player) * ComboMultiplierPerPoint + GetLegendBonus(player);
    }

    public int GetCardsPlayedThisTurn(Player? player)
    {
        return player == null ? 0 : CardsPlayedThisTurnState.GetValueOrDefault(player, 0);
    }

    public int GetAttackCardsThisTurn(Player? player)
    {
        return player == null ? 0 : AttackCardsThisTurnState.GetValueOrDefault(player, 0);
    }

    public int GetSkillCardsThisTurn(Player? player)
    {
        return player == null ? 0 : SkillCardsThisTurnState.GetValueOrDefault(player, 0);
    }

    public CardType? GetLastCardTypeThisTurn(Player? player)
    {
        if (player == null)
        {
            return null;
        }

        int rawValue = LastCardTypeThisTurnState.GetValueOrDefault(player, NoCardType);
        return rawValue == NoCardType ? null : (CardType)rawValue;
    }

    public SerializableCard? GetPreviousTurnFirstCard(Player? player)
    {
        return GetStoredCard(PreviousTurnFirstCardState, player);
    }

    public int GetModifierTokenCount(Player? player)
    {
        return player == null ? 0 : ModifierTokensState.GetValueOrDefault(player, 0);
    }

    public void AddModifierTokens(Player? player, int amount)
    {
        if (player == null || amount <= 0)
        {
            return;
        }

        ModifierTokensState[player] = GetModifierTokenCount(player) + amount;
    }

    public void AddCombo(Player? player, float amount)
    {
        if (player == null || amount <= 0f)
        {
            return;
        }

        SetComboCounter(player, GetComboCounter(player) + amount);
    }

    public void SetComboAtLeast(Player? player, float value)
    {
        if (player == null)
        {
            return;
        }

        SetComboCounter(player, Math.Max(GetComboCounter(player), value));
    }

    public IReadOnlyList<BalatroCardEdition> GetModStationOffers(Player? player)
    {
        return
        [
            NormalizeEditionOffer(player == null ? 0 : ModStationOffer1State.GetValueOrDefault(player, 0)),
            NormalizeEditionOffer(player == null ? 0 : ModStationOffer2State.GetValueOrDefault(player, 0))
        ];
    }

    public int GetEditionShopCost(BalatroCardEdition edition)
    {
        return edition switch
        {
            BalatroCardEdition.Foil => ModStationFoilCost,
            BalatroCardEdition.Holographic => ModStationHolographicCost,
            BalatroCardEdition.Polychrome => ModStationPolychromeCost,
            BalatroCardEdition.Negative => ModStationNegativeCost,
            _ => 0
        };
    }

    public void EnsureModStationOffers(Player player)
    {
        if (RunState.CurrentRoom?.RoomType != RoomType.Shop)
        {
            return;
        }

        if (ModStationFloorState.GetValueOrDefault(player, 0) != RunState.TotalFloor || !HasValidModStationOffers(player))
        {
            RollModStationOffers(player);
        }
    }

    public async Task<bool> RefreshModStationOffers(Player player, bool payRefreshCost)
    {
        if (payRefreshCost && player.Gold < ModStationRefreshCost)
        {
            return false;
        }

        if (payRefreshCost)
        {
            await PlayerCmd.LoseGold(ModStationRefreshCost, player, GoldLossType.Spent);
        }

        RollModStationOffers(player);
        return true;
    }

    public async Task<bool> PurchaseModStationOffer(Player player, BalatroCardEdition edition)
    {
        if (edition == BalatroCardEdition.None)
        {
            return false;
        }

        int cost = GetEditionShopCost(edition);
        bool useToken = GetModifierTokenCount(player) > 0;
        if (!useToken && player.Gold < cost)
        {
            return false;
        }

        CardSelectorPrefs prefs = new(new LocString("gameplay_ui", "YUWANCARD-BALATRO_MOD_STATION.selectionScreenPrompt"), 1)
        {
            Cancelable = true
        };
        CardModel? selected = (await CardSelectCmd.FromDeckGeneric(
                player,
                prefs,
                filter: card => BalatroCardEditionHelper.CanApplyEdition(card, edition)))
            .FirstOrDefault();
        if (selected == null)
        {
            return false;
        }

        if (!BalatroCardEditionHelper.TryApplyEdition(selected, edition))
        {
            return false;
        }

        if (useToken)
        {
            ModifierTokensState[player] = Math.Max(0, GetModifierTokenCount(player) - 1);
        }
        else
        {
            await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
        }

        return true;
    }

    #endregion

    #region Internal Helpers

    private IEnumerable<CardModel> GetBalatroRewardCards(Player player)
    {
        IEnumerable<CardModel> cards = ModelDb.CardPool<BalatroCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);

        foreach (CardModel card in cards.Where(card => card is not Investment
                     and not CompoundInterest
                     and not Dividend
                     and not Bankruptcy
                     and not Inflation))
        {
            yield return card;
        }
    }

    private static List<RelicModel> GetAvailableJokers(Player player)
    {
        return BalatroJokerRelicModel.GetAvailableRewardableJokers(player);
    }

    private static bool HasBalatroRelicReward(IEnumerable<Reward> rewards)
    {
        return rewards
            .OfType<RelicReward>()
            .Select(reward => YuWanReflectionHelper.GetPrivateField<RelicModel>(reward, "_relic"))
            .Any(relic => relic is BalatroRelicModel);
    }

    private static IEnumerable<RelicModel> GetAllJokerModels()
    {
        return BalatroJokerRelicModel.GetRewardableJokers();
    }

    private float CalculateComboGain(Player player, CardModel card)
    {
        if (card.Type is CardType.Status or CardType.Curse)
        {
            if (player.GetRelic<WildCard>() == null)
            {
                return 0f;
            }

            SetLastCardTypeThisTurn(player, card.Type);
            return card.Type == CardType.Curse ? 2f : 0.5f;
        }

        float gain = 1f;
        if (card.Rarity == CardRarity.Uncommon)
        {
            gain += 0.5f;
        }
        else if (card.Rarity is CardRarity.Rare or CardRarity.Ancient)
        {
            gain += 1f;
        }

        if (!card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0)
        {
            gain += 0.5f;
        }

        if (BalatroCardEditionHelper.HasEdition(card))
        {
            gain += 1f;
        }

        CardType? lastCardType = GetLastCardTypeThisTurn(player);
        if (lastCardType.HasValue && lastCardType.Value == card.Type)
        {
            gain += 1f;
        }

        SetLastCardTypeThisTurn(player, card.Type);
        return gain;
    }

    private int GetCompoundInterestCapBonus(Player player)
    {
        return player.Creature.GetPower<CompoundInterestPower>()?.Amount ?? 0;
    }

    private float GetLegendBonus(Player? player)
    {
        if (player == null)
        {
            return 0f;
        }

        LegendJoker? legend = player.GetRelic<LegendJoker>();
        if (legend == null)
        {
            return 0f;
        }

        return legend.GetLegendBonus();
    }

    private static BalatroCardEdition NormalizeEditionOffer(int savedValue)
    {
        return Enum.IsDefined(typeof(BalatroCardEdition), savedValue)
            ? (BalatroCardEdition)savedValue
            : BalatroCardEdition.None;
    }

    private bool HasValidModStationOffers(Player? player)
    {
        IReadOnlyList<BalatroCardEdition> offers = GetModStationOffers(player);
        return offers.All(offer => offer != BalatroCardEdition.None) && offers.Distinct().Count() == offers.Count;
    }

    private void RollModStationOffers(Player player)
    {
        List<BalatroCardEdition> pool =
        [
            BalatroCardEdition.Foil,
            BalatroCardEdition.Foil,
            BalatroCardEdition.Foil,
            BalatroCardEdition.Holographic,
            BalatroCardEdition.Holographic,
            BalatroCardEdition.Holographic,
            BalatroCardEdition.Polychrome,
            BalatroCardEdition.Polychrome,
            BalatroCardEdition.Negative
        ];

        BalatroCardEdition first = DrawEdition(player, pool);
        pool.RemoveAll(edition => edition == first);
        BalatroCardEdition second = DrawEdition(player, pool);
        if (second == BalatroCardEdition.None)
        {
            second = first == BalatroCardEdition.Foil
                ? BalatroCardEdition.Holographic
                : BalatroCardEdition.Foil;
        }

        ModStationOffer1State[player] = (int)first;
        ModStationOffer2State[player] = (int)second;
        ModStationFloorState[player] = RunState.TotalFloor;
    }

    private static BalatroCardEdition DrawEdition(Player player, List<BalatroCardEdition> pool)
    {
        if (pool.Count == 0)
        {
            return BalatroCardEdition.None;
        }

        int index = player.PlayerRng.Shops.NextInt(pool.Count);
        return pool[index];
    }

    private async Task ApplyInterestForRoom(Player player)
    {
        int lastInterestFloor = LastInterestFloorState.GetValueOrDefault(player, 0);

        if (RunState.TotalFloor <= 0 || RunState.TotalFloor <= lastInterestFloor)
        {
            return;
        }

        LastInterestFloorState[player] = RunState.TotalFloor;

        int interest = (int)Math.Floor(player.Gold * InterestRate);
        if (interest <= 0)
        {
            return;
        }

        interest = Math.Min(interest, BaseInterestCap + GetCompoundInterestCapBonus(player));

        BankerJoker? bankerJoker = player.GetRelic<BankerJoker>();
        int bankerBonus = bankerJoker?.GetInterestBonusGold() ?? 0;
        interest += bankerBonus;

        if (interest <= 0)
        {
            return;
        }

        if (bankerBonus > 0)
        {
            bankerJoker!.Flash();
        }

        await PlayerCmd.GainGold(interest, player);
    }

    private void ResetCombatState()
    {
        foreach (Player player in RunState.Players)
        {
            ResetCombatState(player);
        }

        ActiveTurnPlayerNetIdState.Remove(RunState);
    }

    private void ResetCombatState(Player player)
    {
        SetComboCounter(player, 0f);
        SetRetainedCombo(player, 0f);
        LastInterestFloorState[player] = RunState.TotalFloor;
        SetCurrentTurnFirstCard(player, null);
        SetPreviousTurnFirstCard(player, null);
        ResetTurnState(player);
    }

    private void ResetTurnState(Player? player)
    {
        if (player == null)
        {
            return;
        }

        SetComboCounter(player, 0f);
        CardsPlayedThisTurnState.Remove(player);
        AttackCardsThisTurnState.Remove(player);
        SkillCardsThisTurnState.Remove(player);
        LastCardTypeThisTurnState.Remove(player);
        SetCurrentTurnFirstCard(player, null);
    }

    private static float GetScaledState(SavedAttachedState<Player, int> state, Player? player)
    {
        if (player == null)
        {
            return 0f;
        }

        return state.GetValueOrDefault(player, 0) / RetainedComboScale;
    }

    private static void SetScaledState(SavedAttachedState<Player, int> state, Player? player, float value)
    {
        if (player == null)
        {
            return;
        }

        int scaledValue = (int)MathF.Round(Math.Clamp(value, 0f, MaxCombo) * RetainedComboScale);
        if (scaledValue <= 0)
        {
            state.Remove(player);
            return;
        }

        state[player] = scaledValue;
    }

    private float GetRetainedCombo(Player? player)
    {
        return GetScaledState(RetainedComboScaledState, player);
    }

    private void SetRetainedCombo(Player? player, float value)
    {
        SetScaledState(RetainedComboScaledState, player, value);
    }

    private void SetComboCounter(Player? player, float value)
    {
        SetScaledState(ComboCounterScaledState, player, value);
    }

    private static SerializableCard? GetStoredCard(SavedAttachedState<Player, SerializableCard> state, Player? player)
    {
        if (player == null)
        {
            return null;
        }

        return state.TryGetValue(player, out SerializableCard? card) ? card : null;
    }

    private static void SetStoredCard(SavedAttachedState<Player, SerializableCard> state, Player? player, SerializableCard? card)
    {
        if (player == null)
        {
            return;
        }

        if (card == null)
        {
            state.Remove(player);
            return;
        }

        state[player] = card;
    }

    private SerializableCard? GetCurrentTurnFirstCard(Player? player)
    {
        return GetStoredCard(CurrentTurnFirstCardState, player);
    }

    private void SetCurrentTurnFirstCard(Player? player, SerializableCard? card)
    {
        SetStoredCard(CurrentTurnFirstCardState, player, card);
    }

    private void SetPreviousTurnFirstCard(Player? player, SerializableCard? card)
    {
        SetStoredCard(PreviousTurnFirstCardState, player, card);
    }

    private void SetLastCardTypeThisTurn(Player? player, CardType? cardType)
    {
        if (player == null)
        {
            return;
        }

        if (!cardType.HasValue)
        {
            LastCardTypeThisTurnState.Remove(player);
            return;
        }

        LastCardTypeThisTurnState[player] = (int)cardType.Value;
    }

    private void SetActiveTurnPlayer(Player? player)
    {
        if (player == null)
        {
            ActiveTurnPlayerNetIdState.Remove(RunState);
            return;
        }

        ActiveTurnPlayerNetIdState[RunState] = player.NetId.ToString();
    }

    private Player? GetActiveTurnPlayer()
    {
        if (!ActiveTurnPlayerNetIdState.TryGetValue(RunState, out string? activeNetId)
            || string.IsNullOrWhiteSpace(activeNetId)
            || !ulong.TryParse(activeNetId, out ulong parsedNetId))
        {
            return null;
        }

        return RunState.Players.FirstOrDefault(player => player.NetId == parsedNetId);
    }
    #endregion
}
