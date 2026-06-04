using System.Text.Json;
using Godot;
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
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;
using YuWanCard.Relics;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Modifiers;

public sealed class BalatroModifier : YuWanModifierModel
{
    private const string BagSeparator = "|";

    public override bool AllowedInCustomRun => false;

    [SavedProperty]
    public int YUWANCARD_UnlockedJokerSlots { get; set; } = 3;

    [SavedProperty]
    public int YUWANCARD_RetainedComboScaled { get; set; }

    [SavedProperty]
    public int YUWANCARD_LastInterestFloor { get; set; }

    [SavedProperty]
    public string YUWANCARD_JokerBag { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_JokerSlot1Id { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_JokerSlot2Id { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_JokerSlot3Id { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_JokerSlot4Id { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_JokerSlot5Id { get; set; } = string.Empty;

    [SavedProperty]
    public string YUWANCARD_JokerSlot6Id { get; set; } = string.Empty;

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

    public float ComboCounter { get; private set; }

    public int CardsPlayedThisTurn { get; private set; }

    private int AttackCardsThisTurn { get; set; }

    private int SkillCardsThisTurn { get; set; }

    private CardType? LastCardTypeThisTurn { get; set; }

    public override bool AllowedInDailyRun => false;

    public override IEnumerable<IHoverTip> HoverTips =>
    [
        new HoverTip(
            new LocString("modifiers", "YUWANCARD-BALATRO.title"),
            new LocString("modifiers", "YUWANCARD-BALATRO.description"))
    ];

    protected override void AfterRunCreated(RunState runState)
    {
        base.AfterRunCreated(runState);
        MainFile.Logger.Info(
            $"[BalatroDebug] BalatroModifier.AfterRunCreated seed={runState.Rng.StringSeed} players={runState.Players.Count} act0={runState.Acts.FirstOrDefault()?.Id.Entry ?? "null"} modifiers=[{string.Join(", ", runState.Modifiers.Select(static m => m.Id.Entry))}]");
    }

    public float ComboMultiplier => 1f + ComboCounter * 0.1f + GetLegendBonus();

    private float RetainedCombo
    {
        get => YUWANCARD_RetainedComboScaled / 20f;
        set => YUWANCARD_RetainedComboScaled = (int)MathF.Round(Math.Clamp(value, 0f, 30f) * 20f);
    }

    private SerializableCard? CurrentTurnFirstCard
    {
        get => DeserializeStoredCard(YUWANCARD_CurrentTurnFirstCardJson);
        set => YUWANCARD_CurrentTurnFirstCardJson = SerializeStoredCard(value);
    }

    private SerializableCard? PreviousTurnFirstCard
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

        if (room.RoomType != RoomType.Boss)
        {
            return;
        }

        int currentAct = RunState.CurrentActIndex;
        if (currentAct == 0)
        {
            YUWANCARD_UnlockedJokerSlots = Math.Max(YUWANCARD_UnlockedJokerSlots, 4);
        }
        else if (currentAct == 1)
        {
            YUWANCARD_UnlockedJokerSlots = Math.Max(YUWANCARD_UnlockedJokerSlots, 5);
        }

        TryAutoEquipFromBag();
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

        int interest = (int)Math.Floor(player.Gold * 0.1m);
        if (interest <= 0)
        {
            return;
        }

        interest = Math.Min(interest, 10 + GetCompoundInterestCapBonus(player));
        interest += 3 * CountEffectiveJokers<BankerJoker>(player);

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

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != GetBalatroPlayer())
        {
            return;
        }

        if (RetainedCombo > 0f)
        {
            ComboCounter = Math.Min(30f, RetainedCombo);
            RetainedCombo = 0f;
        }

        if (player.GetRelic<Dice>() != null)
        {
            int roll = RunState.Rng.Niche.NextInt(1, 4);
            ComboCounter = Math.Max(ComboCounter, roll);
        }

        if (player.GetRelic<Chip>() != null)
        {
            AddCombo(3f);
        }

        int collectorEnergy = GetCollectorEnergy(player);
        if (collectorEnergy > 0)
        {
            await PlayerCmd.GainEnergy(collectorEnergy, player);
        }

        SerializableCard? previousTurnFirstCard = PreviousTurnFirstCard;
        if (previousTurnFirstCard != null)
        {
            CombatState? combatState = player.Creature.CombatState;
            if (combatState == null)
            {
                return;
            }

            CardModel copy = CardModel.FromSerializable(previousTurnFirstCard);
            combatState.AddCard(copy, player);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, addedByPlayer: true);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || side != player.Creature.Side)
        {
            return;
        }

        float retainRatio = 0f;
        if (ComboCounter >= 20f)
        {
            retainRatio = 0.1f;
            await PowerCmd.Apply<InertiaPower>(player.Creature, 1, player.Creature, null);
        }

        if (player.GetRelic<SteelJoker>() != null)
        {
            retainRatio = Math.Max(retainRatio, 0.2f);
        }

        RetainedCombo = retainRatio > 0f
            ? MathF.Min(30f, ComboCounter * retainRatio)
            : 0f;

        PreviousTurnFirstCard = CurrentTurnFirstCard;
        ResetTurnState();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || cardPlay.Card.Owner != player)
        {
            return;
        }

        CardModel card = cardPlay.Card;
        if (CurrentTurnFirstCard == null)
        {
            CurrentTurnFirstCard = card.ToSerializable();
        }

        float comboGain = CalculateComboGain(player, card);
        if (comboGain <= 0f)
        {
            return;
        }

        AddCombo(comboGain);
        CardsPlayedThisTurn++;

        if (card.Type == CardType.Attack)
        {
            AttackCardsThisTurn++;
            int greedTriggers = CountEffectiveJokers<GreedJoker>(player);
            if (greedTriggers > 0 && AttackCardsThisTurn % 3 == 0)
            {
                await PlayerCmd.GainGold(5 * greedTriggers, player);
            }
        }
        else if (card.Type == CardType.Skill)
        {
            SkillCardsThisTurn++;
            int gluttonyTriggers = CountEffectiveJokers<GluttonyJoker>(player);
            if (gluttonyTriggers > 0 && SkillCardsThisTurn % 4 == 0)
            {
                await CreatureCmd.Heal(player.Creature, 3 * gluttonyTriggers);
            }
        }

        if (CountEffectiveJokers<GamblerJoker>(player) > 0 && ComboCounter >= 5f)
        {
            Creature? target = OwnerCreatureCombatState(player)?.Enemies
                .Where(enemy => !enemy.IsDead)
                .OrderBy(_ => RunState.Rng.Niche.NextFloat())
                .FirstOrDefault();
            if (target != null)
            {
                int damage = RunState.Rng.Niche.NextInt(8, 21);
                await CreatureCmd.Damage(context, target, damage, ValueProp.Move, player.Creature, null);
            }
        }
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || card.Owner != player)
        {
            return playCount;
        }

        int extra = 0;
        if (LastCardTypeThisTurn.HasValue && LastCardTypeThisTurn.Value == card.Type)
        {
            extra += CountEffectiveJokers<MirrorJoker>(player);
        }

        if (BalatroCardEditionHelper.HasEdition(card))
        {
            extra += BalatroCardEditionHelper.GetPlayCountBonus(card);
            extra += CountEffectiveJokers<PolychromeJoker>(player);
        }

        if (player.GetRelic<LuckyCard>() != null && (CardsPlayedThisTurn + 1) % 7 == 0)
        {
            extra += 2;
        }

        return playCount + extra;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        Player? player = GetBalatroPlayer();
        if (player == null || dealer != player.Creature || cardSource?.Type != CardType.Attack)
        {
            return 0m;
        }

        int jokerCount = CountEffectiveJokers<MiserJoker>(player);
        if (jokerCount <= 0)
        {
            return 0m;
        }

        int zeroCostCount = PileType.Hand.GetPile(player).Cards
            .Count(card => !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0);
        return zeroCostCount * jokerCount;
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
            List<RelicModel> available = GetAvailableJokers();
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

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        return player == GetBalatroPlayer();
    }

    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        int refundMultiplier = CountEffectiveJokers<InvestorJoker>(player);
        if (refundMultiplier <= 0)
        {
            return;
        }

        int refund = (int)Math.Floor(goldSpent * 0.2m * refundMultiplier);
        if (refund > 0)
        {
            await PlayerCmd.GainGold(refund, player);
        }
    }

    public async Task AcquireJoker(YuWanJokerRelicModel joker, Player player)
    {
        string jokerId = joker.Id.Entry;
        if (string.IsNullOrEmpty(jokerId) || HasJoker(jokerId))
        {
            return;
        }

        if (joker is NegativeJoker)
        {
            YUWANCARD_UnlockedJokerSlots = Math.Max(YUWANCARD_UnlockedJokerSlots, 5);
        }

        if (!TryEquipJoker(jokerId, joker is NegativeJoker ? 6 : GetCurrentJokerCapacity()))
        {
            List<string> bag = GetJokerBag();
            bag.Add(jokerId);
            SaveJokerBag(bag);
        }

        TryAutoEquipFromBag();
        await Task.CompletedTask;
    }

    public IReadOnlyList<string> GetEquippedJokerIds()
    {
        return GetSlotIds()
            .Take(GetCurrentJokerCapacity())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();
    }

    public int GetCurrentJokerCapacity()
    {
        int unlocked = Math.Clamp(YUWANCARD_UnlockedJokerSlots, 3, 5);
        if (HasStoredJoker(ModelDb.GetId<NegativeJoker>().Entry))
        {
            return 6;
        }

        return unlocked;
    }

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

    public string GetJokerDisplayText()
    {
        List<string> slots = [];
        int capacity = GetCurrentJokerCapacity();
        string[] equipped = GetSlotIds().Take(capacity).Select(id => string.IsNullOrWhiteSpace(id) ? "-" : ResolveJokerShortName(id!)).ToArray();
        for (int i = 0; i < capacity; i++)
        {
            slots.Add($"[{equipped[i]}]");
        }

        return "JOKER " + string.Join(" ", slots);
    }

    public IReadOnlyList<string> GetJokerBagIds()
    {
        return GetJokerBag();
    }

    public IReadOnlyList<string> GetAllJokerSlotIds()
    {
        return GetSlotIds();
    }

    public bool IsJokerSlotUnlocked(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < GetCurrentJokerCapacity();
    }

    public string GetJokerTitle(string jokerId)
    {
        if (string.IsNullOrWhiteSpace(jokerId))
        {
            return "-";
        }

        RelicModel? relic = ResolveJokerModel(jokerId);
        return relic?.Title.GetFormattedText() ?? ResolveJokerShortName(jokerId);
    }

    public string GetJokerDescription(string jokerId)
    {
        if (string.IsNullOrWhiteSpace(jokerId))
        {
            return string.Empty;
        }

        RelicModel? relic = ResolveJokerModel(jokerId);
        return relic?.DynamicDescription.GetFormattedText() ?? string.Empty;
    }

    public Texture2D? GetJokerIcon(string jokerId)
    {
        if (string.IsNullOrWhiteSpace(jokerId))
        {
            return null;
        }

        return ResolveJokerModel(jokerId)?.Icon;
    }

    public bool TryEquipBagJoker(string jokerId, int slotIndex)
    {
        if (!IsJokerSlotUnlocked(slotIndex))
        {
            return false;
        }

        List<string> bag = GetJokerBag();
        int bagIndex = bag.FindIndex(id => string.Equals(id, jokerId, StringComparison.Ordinal));
        if (bagIndex < 0)
        {
            return false;
        }

        string[] slots = GetSlotIds();
        string existing = slots[slotIndex];
        if (string.Equals(existing, jokerId, StringComparison.Ordinal))
        {
            return false;
        }

        bag.RemoveAt(bagIndex);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            bag.Add(existing);
        }

        SetSlotId(slotIndex, jokerId);
        SaveJokerBag(bag);
        return true;
    }

    public bool TryUnequipJoker(int slotIndex)
    {
        if (!IsJokerSlotUnlocked(slotIndex))
        {
            return false;
        }

        string[] slots = GetSlotIds();
        string jokerId = slots[slotIndex];
        if (string.IsNullOrWhiteSpace(jokerId))
        {
            return false;
        }

        List<string> bag = GetJokerBag();
        bag.Add(jokerId);
        SetSlotId(slotIndex, string.Empty);
        SaveJokerBag(bag);
        return true;
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
            BalatroCardEdition.Foil => 75,
            BalatroCardEdition.Holographic => 75,
            BalatroCardEdition.Polychrome => 150,
            BalatroCardEdition.Negative => 250,
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
        const int refreshCost = 25;
        if (payRefreshCost && player.Gold < refreshCost)
        {
            return false;
        }

        if (payRefreshCost)
        {
            await PlayerCmd.LoseGold(refreshCost, player, GoldLossType.Spent);
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

    private IEnumerable<CardModel> GetBalatroRewardCards(Player player)
    {
        IEnumerable<CardModel> cards = ModelDb.CardPool<BalatroCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);

        if (player.Character.CardPool is PigCardPool)
        {
            foreach (CardModel card in cards)
            {
                yield return card;
            }

            yield break;
        }

        foreach (CardModel card in cards.Where(card => card is not Investment
                     and not CompoundInterest
                     and not Dividend
                     and not Bankruptcy
                     and not Inflation))
        {
            yield return card;
        }
    }

    private List<RelicModel> GetAvailableJokers()
    {
        HashSet<string> ownedIds = GetEquippedJokerIds()
            .Concat(GetJokerBag())
            .ToHashSet(StringComparer.Ordinal);

        return GetAllJokerModels()
            .Where(relic => !ownedIds.Contains(relic.Id.Entry))
            .ToList();
    }

    private RelicModel? ResolveJokerModel(string jokerId)
    {
        return GetAllJokerModels()
            .FirstOrDefault(model => string.Equals(model.Id.Entry, jokerId, StringComparison.Ordinal));
    }

    private IEnumerable<RelicModel> GetAllJokerModels()
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

    private void TryAutoEquipFromBag()
    {
        List<string> bag = GetJokerBag();
        if (bag.Count == 0)
        {
            return;
        }

        bool changed = false;
        while (bag.Count > 0 && TryEquipJoker(bag[0], GetCurrentJokerCapacity()))
        {
            bag.RemoveAt(0);
            changed = true;
        }

        if (changed)
        {
            SaveJokerBag(bag);
        }
    }

    private bool TryEquipJoker(string jokerId, int capacity)
    {
        string[] slots = GetSlotIds();
        for (int i = 0; i < Math.Min(capacity, slots.Length); i++)
        {
            if (!string.IsNullOrWhiteSpace(slots[i]))
            {
                continue;
            }

            SetSlotId(i, jokerId);
            return true;
        }

        return false;
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

    private int GetCollectorEnergy(Player player)
    {
        int collectorCount = CountEffectiveJokers<CollectorJoker>(player);
        if (collectorCount <= 0)
        {
            return 0;
        }

        int rareCount = player.Deck.Cards.Count(card =>
            card.Rarity is CardRarity.Rare or CardRarity.Ancient);
        return rareCount / 5 * collectorCount;
    }

    private int CountEffectiveJokers<T>(Player player) where T : RelicModel
    {
        string id = ModelDb.GetId<T>().Entry;
        int count = GetEffectiveJokerIds(player).Count(jokerId => jokerId == id);
        return count;
    }

    private IReadOnlyList<string> GetEffectiveJokerIds(Player player)
    {
        List<string> ids = GetEquippedJokerIds().ToList();
        if (player.GetRelic<Blueprint>() != null)
        {
            string? rightmost = ids.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(rightmost))
            {
                ids.Add(rightmost);
            }
        }

        return ids;
    }

    private float GetLegendBonus()
    {
        Player? player = GetBalatroPlayer();
        if (player == null)
        {
            return 0f;
        }

        return CardsPlayedThisTurn * 0.2f * CountEffectiveJokers<LegendJoker>(player);
    }

    private Player? GetBalatroPlayer()
    {
        return LocalContext.GetMe(RunState) ?? RunState.Players.FirstOrDefault();
    }

    private static CombatState? OwnerCreatureCombatState(Player player)
    {
        return player.Creature?.CombatState;
    }

    private bool HasJoker(string jokerId)
    {
        return GetEquippedJokerIds().Contains(jokerId, StringComparer.Ordinal)
            || GetJokerBag().Contains(jokerId, StringComparer.Ordinal);
    }

    private bool HasStoredJoker(string jokerId)
    {
        return GetSlotIds().Any(id => string.Equals(id, jokerId, StringComparison.Ordinal))
            || GetJokerBag().Contains(jokerId, StringComparer.Ordinal);
    }

    private List<string> GetJokerBag()
    {
        return string.IsNullOrWhiteSpace(YUWANCARD_JokerBag)
            ? []
            : YUWANCARD_JokerBag
                .Split(BagSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
    }

    private void SaveJokerBag(List<string> bag)
    {
        YUWANCARD_JokerBag = string.Join(BagSeparator, bag);
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
        ComboCounter = Math.Clamp(ComboCounter + amount, 0f, 30f);
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

    private string ResolveJokerShortName(string jokerId)
    {
        return jokerId.Replace("YUWANCARD-", string.Empty, StringComparison.Ordinal)
            .Replace("_JOKER", string.Empty, StringComparison.Ordinal)
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "?";
    }

    private string[] GetSlotIds()
    {
        return
        [
            YUWANCARD_JokerSlot1Id,
            YUWANCARD_JokerSlot2Id,
            YUWANCARD_JokerSlot3Id,
            YUWANCARD_JokerSlot4Id,
            YUWANCARD_JokerSlot5Id,
            YUWANCARD_JokerSlot6Id
        ];
    }

    private void SetSlotId(int index, string jokerId)
    {
        switch (index)
        {
            case 0:
                YUWANCARD_JokerSlot1Id = jokerId;
                break;
            case 1:
                YUWANCARD_JokerSlot2Id = jokerId;
                break;
            case 2:
                YUWANCARD_JokerSlot3Id = jokerId;
                break;
            case 3:
                YUWANCARD_JokerSlot4Id = jokerId;
                break;
            case 4:
                YUWANCARD_JokerSlot5Id = jokerId;
                break;
            case 5:
                YUWANCARD_JokerSlot6Id = jokerId;
                break;
        }
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
}
