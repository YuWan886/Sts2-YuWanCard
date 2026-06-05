using System.Text.Json;
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
using YuWanCard.Powers;
using YuWanCard.Relics;

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

    // Mod Station
    private const int ModStationRefreshCost = 25;
    private const int ModStationFoilCost = 75;
    private const int ModStationHolographicCost = 75;
    private const int ModStationPolychromeCost = 150;
    private const int ModStationNegativeCost = 250;

    #region Saved Properties

    public override bool AllowedInCustomRun => true;

    [SavedProperty]
    public int YUWANCARD_RetainedComboScaled { get; set; }

    [SavedProperty]
    public int YUWANCARD_LastInterestFloor { get; set; }

    [SavedProperty]
    public string YUWANCARD_CurrentTurnFirstCardJson { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_PreviousTurnFirstCardJson { get; set; } = string.Empty;

    [SavedProperty]
    public int YUWANCARD_ModifierTokens { get; set; }

    [SavedProperty]
    public int YUWANCARD_ModStationOffer1 { get; set; }

    [SavedProperty]
    public int YUWANCARD_ModStationOffer2 { get; set; }

    [SavedProperty]
    public int YUWANCARD_ModStationFloor { get; set; }

    #endregion

    #region Runtime State

    public float ComboCounter { get; set; }

    public int CardsPlayedThisTurn { get; private set; }

    public int AttackCardsThisTurn { get; private set; }

    public int SkillCardsThisTurn { get; private set; }

    public CardType? LastCardTypeThisTurn { get; private set; }

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
        MainFile.Logger.Info(
            $"[BalatroDebug] BalatroModifier.AfterRunCreated seed={runState.Rng.StringSeed} players={runState.Players.Count} act0={runState.Acts.FirstOrDefault()?.Id.Entry ?? "null"} modifiers=[{string.Join(", ", runState.Modifiers.Select(static m => m.Id.Entry))}]");
    }

    public float ComboMultiplier => 1f + ComboCounter * ComboMultiplierPerPoint + GetLegendBonus();

    private float RetainedCombo
    {
        get => YUWANCARD_RetainedComboScaled / RetainedComboScale;
        set => YUWANCARD_RetainedComboScaled = (int)MathF.Round(Math.Clamp(value, 0f, MaxCombo) * RetainedComboScale);
    }

    private SerializableCard? CurrentTurnFirstCard
    {
        get => DeserializeStoredCard(YUWANCARD_CurrentTurnFirstCardJson);
        set => YUWANCARD_CurrentTurnFirstCardJson = SerializeStoredCard(value);
    }

    public SerializableCard? PreviousTurnFirstCard
    {
        get => DeserializeStoredCard(YUWANCARD_PreviousTurnFirstCardJson);
        set => YUWANCARD_PreviousTurnFirstCardJson = SerializeStoredCard(value);
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

        MainFile.Logger.Info(
            $"[BalatroDebug] BalatroModifier.AfterRoomEntered roomType={room.RoomType} totalFloor={RunState.TotalFloor} currentAct={RunState.CurrentActIndex} lastInterestFloor={YUWANCARD_LastInterestFloor}");

        Player? player = GetBalatroPlayer();
        if (player == null)
        {
            MainFile.Logger.Info("[BalatroDebug] BalatroModifier.AfterRoomEntered aborted: player is null.");
            return;
        }

        if (RunState.TotalFloor <= 0 || RunState.TotalFloor <= YUWANCARD_LastInterestFloor)
        {
            MainFile.Logger.Info(
                $"[BalatroDebug] BalatroModifier.AfterRoomEntered skipped interest: totalFloor={RunState.TotalFloor}, lastInterestFloor={YUWANCARD_LastInterestFloor}.");
            return;
        }

        YUWANCARD_LastInterestFloor = RunState.TotalFloor;

        int interest = (int)Math.Floor(player.Gold * InterestRate);
        if (interest <= 0)
        {
            return;
        }

        interest = Math.Min(interest, BaseInterestCap + GetCompoundInterestCapBonus(player));
        interest += 3 * SimpleJokerCount<BankerJoker>(player);

        if (interest > 0)
        {
            MainFile.Logger.Info(
                $"[BalatroDebug] BalatroModifier.AfterRoomEntered granting interest={interest} currentGold={player.Gold}.");
            await PlayerCmd.GainGold(interest, player);
            MainFile.Logger.Info(
                $"[BalatroDebug] BalatroModifier.AfterRoomEntered gain complete newGold={player.Gold}.");
        }
        else
        {
            MainFile.Logger.Info(
                $"[BalatroDebug] BalatroModifier.AfterRoomEntered computed non-positive interest={interest}.");
        }
    }

    #endregion

    #region Turn & Card Hooks

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != GetBalatroPlayer())
        {
            return;
        }

        if (RetainedCombo > 0f)
        {
            ComboCounter = Math.Min(MaxCombo, RetainedCombo);
            RetainedCombo = 0f;
        }

        SerializableCard? previousTurnFirstCard = PreviousTurnFirstCard;
        if (previousTurnFirstCard != null)
        {
            ICombatState? combatState = player.Creature.CombatState;
            if (combatState == null)
            {
                return;
            }

            CardModel copy = CardModel.FromSerializable(previousTurnFirstCard);
            combatState.AddCard(copy, player);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || side != player.Creature.Side)
        {
            return;
        }

        float retainRatio = 0f;
        if (ComboCounter >= RetainedComboThreshold)
        {
            retainRatio = DefaultRetainRatio;
            await PowerCmd.Apply<InertiaPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
        }

        if (player.GetRelic<SteelJoker>() != null)
        {
            retainRatio = Math.Max(retainRatio, SteelJokerRetainRatio);
        }

        RetainedCombo = retainRatio > 0f
            ? MathF.Min(MaxCombo, ComboCounter * retainRatio)
            : 0f;

        PreviousTurnFirstCard = CurrentTurnFirstCard;
        ResetTurnState();
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || cardPlay.Card.Owner != player)
        {
            return Task.CompletedTask;
        }

        CardModel card = cardPlay.Card;
        if (CurrentTurnFirstCard == null)
        {
            CurrentTurnFirstCard = card.ToSerializable();
        }

        float comboGain = CalculateComboGain(player, card);
        if (comboGain <= 0f)
        {
            return Task.CompletedTask;
        }

        AddCombo(comboGain);
        CardsPlayedThisTurn++;

        if (card.Type == CardType.Attack)
        {
            AttackCardsThisTurn++;
        }
        else if (card.Type == CardType.Skill)
        {
            SkillCardsThisTurn++;
        }

        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || card.Owner != player)
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
        Player? player = GetBalatroPlayer();
        if (player == null || dealer != player.Creature || cardSource?.Type != CardType.Attack)
        {
            return 1m;
        }

        return (decimal)ComboMultiplier;
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
        float chance = room.RoomType == RoomType.Boss ? 1f : 0.25f;
        if (RunState.Rng.Niche.NextFloat() <= chance)
        {
            List<RelicModel> available = GetAvailableJokers(player);
            if (available.Count > 0)
            {
                RelicModel reward = available[RunState.Rng.Niche.NextInt(available.Count)];
                rewards.Add(new RelicReward(reward.ToMutable(), player));
                modified = true;
            }
        }

        float tokenChance = room.RoomType == RoomType.Boss ? 0.5f : 0.2f;
        if (RunState.Rng.Niche.NextFloat() <= tokenChance)
        {
            rewards.Add(new RelicReward(ModelDb.Relic<ModifierToken>().ToMutable(), player));
            modified = true;
        }

        return modified;
    }

    public override decimal ModifyGoldGained(Player player, decimal amount)
    {
        return player == GetBalatroPlayer() ? amount : 0m;
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

    public string GetComboDisplayText()
    {
        return $"COMBO {ComboCounter:0.#}  MULT x{ComboMultiplier:0.0}";
    }

    public int ModifierTokenCount => YUWANCARD_ModifierTokens;

    public void AddModifierTokens(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        YUWANCARD_ModifierTokens += amount;
    }

    public IReadOnlyList<BalatroCardEdition> GetModStationOffers()
    {
        return
        [
            NormalizeEditionOffer(YUWANCARD_ModStationOffer1),
            NormalizeEditionOffer(YUWANCARD_ModStationOffer2)
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

        if (YUWANCARD_ModStationFloor != RunState.TotalFloor || !HasValidModStationOffers())
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
        bool useToken = ModifierTokenCount > 0;
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
            YUWANCARD_ModifierTokens = Math.Max(0, YUWANCARD_ModifierTokens - 1);
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
        return GetAllJokerModels()
            .Where(relic => !player.Relics.Any(r => r.Id == relic.Id))
            .ToList();
    }

    private static IEnumerable<RelicModel> GetAllJokerModels()
    {
        yield return ModelDb.Relic<GreedJoker>();
        yield return ModelDb.Relic<GluttonyJoker>();
        yield return ModelDb.Relic<MirrorJoker>();
        yield return ModelDb.Relic<MiserJoker>();
        yield return ModelDb.Relic<CollectorJoker>();
        yield return ModelDb.Relic<GamblerJoker>();
        yield return ModelDb.Relic<PolychromeJoker>();
        yield return ModelDb.Relic<NegativeJoker>();
        yield return ModelDb.Relic<LegendJoker>();
        yield return ModelDb.Relic<HolographicJoker>();
        yield return ModelDb.Relic<BankerJoker>();
        yield return ModelDb.Relic<InvestorJoker>();
    }

    private float CalculateComboGain(Player player, CardModel card)
    {
        if (card.Type is CardType.Status or CardType.Curse)
        {
            if (player.GetRelic<WildCard>() == null)
            {
                return 0f;
            }

            LastCardTypeThisTurn = card.Type;
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

        if (LastCardTypeThisTurn.HasValue && LastCardTypeThisTurn.Value == card.Type)
        {
            gain += 1f;
        }

        LastCardTypeThisTurn = card.Type;
        return gain;
    }

    private int GetCompoundInterestCapBonus(Player player)
    {
        return player.Creature.GetPower<CompoundInterestPower>()?.Amount ?? 0;
    }

    private float GetLegendBonus()
    {
        Player? player = GetBalatroPlayer();
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

    private static int SimpleJokerCount<T>(Player player) where T : RelicModel
    {
        return player.GetRelic<T>() != null ? 1 : 0;
    }

    private Player? GetBalatroPlayer()
    {
        return LocalContext.GetMe(RunState) ?? RunState.Players.FirstOrDefault();
    }

    private static BalatroCardEdition NormalizeEditionOffer(int savedValue)
    {
        return Enum.IsDefined(typeof(BalatroCardEdition), savedValue)
            ? (BalatroCardEdition)savedValue
            : BalatroCardEdition.None;
    }

    private bool HasValidModStationOffers()
    {
        IReadOnlyList<BalatroCardEdition> offers = GetModStationOffers();
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

        YUWANCARD_ModStationOffer1 = (int)first;
        YUWANCARD_ModStationOffer2 = (int)second;
        YUWANCARD_ModStationFloor = RunState.TotalFloor;
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

    private void AddCombo(float amount)
    {
        ComboCounter = Math.Clamp(ComboCounter + amount, 0f, MaxCombo);
    }

    private void ResetCombatState()
    {
        ComboCounter = 0f;
        RetainedCombo = 0f;
        YUWANCARD_LastInterestFloor = RunState.TotalFloor;
        CurrentTurnFirstCard = null;
        PreviousTurnFirstCard = null;
        ResetTurnState();
    }

    private void ResetTurnState()
    {
        ComboCounter = 0f;
        CardsPlayedThisTurn = 0;
        AttackCardsThisTurn = 0;
        SkillCardsThisTurn = 0;
        LastCardTypeThisTurn = null;
        CurrentTurnFirstCard = null;
    }

    private static string SerializeStoredCard(SerializableCard? card)
    {
        return card == null ? string.Empty : JsonSerializer.Serialize(card);
    }

    private static SerializableCard? DeserializeStoredCard(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SerializableCard>(json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"BalatroModifier: failed to deserialize stored card state: {ex.Message}");
            return null;
        }
    }

    #endregion
}
