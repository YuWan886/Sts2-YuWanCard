using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;
using System.Runtime.CompilerServices;

namespace YuWanCard.Modifiers;

public class VakuuTowerModifier : YuWanModifierModel
{
    private const int MaxCardsToPlay = 500;
    
    private const float LowHpThreshold = 0.30f;
    private const float MediumHpThreshold = 0.60f;
    private const float LowHpBuffer = 0.05f;

    private static class ScoreWeights
    {
        public const int ZeroCostBonus = 150;
        public const int FinisherBonus = 300;
        public const int SurvivalBase = 100;
        public const int ControlBase = 80;
        public const int OffensiveBase = 60;
        public const int EnergyEfficiencyBonus = 50;
        public const int ComboBonus = 40;
        public const int DebuffBase = 120;
        public const int BuffBase = 100;
        public const int BlockBase = 70;
        public const int DamageBase = 60;
        public const int HealBase = 90;
        public const int CardDrawBase = 80;
        
        public const int ZeroCostThreshold = 100;
        public const int FinisherThreshold = 200;
    }

    private static class Multipliers
    {
        public const float LowHpSurvival = 2.5f;
        public const float LowHpControl = 1.5f;
        public const float LowHpOffensive = 0.4f;
        public const float MediumHpSurvival = 1.0f;
        public const float MediumHpControl = 1.0f;
        public const float MediumHpOffensive = 0.7f;
        public const float HighHpSurvival = 0.8f;
        public const float HighHpControl = 0.9f;
        public const float HighHpOffensive = 1.0f;
    }
    
    private enum BloodHealthState
    {
        High,
        Medium,
        Low
    }
    
    private enum CardPriorityLevel
    {
        Survival,
        Control,
        Offensive,
        Utility
    }
    
    private static readonly HashSet<string> SurvivalCardTags = new()
    {
        "Defend",
        "Heal",
        "Block",
        "Regen",
        "Intangible",
        "Buffer",
        "Artifact"
    };
    
    private static readonly HashSet<string> ControlCardTags = new()
    {
        "Weak",
        "Vulnerable",
        "Stun",
        "Silence",
        "Taunt",
        "Freeze",
        "Sleep"
    };
    
    private static readonly Dictionary<string, CardPriorityLevel> PowerToPriority = new()
    {
        { "IntangiblePower", CardPriorityLevel.Survival },
        { "BufferPower", CardPriorityLevel.Survival },
        { "ArtifactPower", CardPriorityLevel.Survival },
        { "RegenPower", CardPriorityLevel.Survival },
        { "PlatingPower", CardPriorityLevel.Survival },
        { "WeakPower", CardPriorityLevel.Control },
        { "VulnerablePower", CardPriorityLevel.Control },
        { "FrailPower", CardPriorityLevel.Control },
        { "StrengthPower", CardPriorityLevel.Offensive },
        { "DexterityPower", CardPriorityLevel.Offensive },
        { "FocusPower", CardPriorityLevel.Offensive },
        { "ThornsPower", CardPriorityLevel.Offensive },
        { "VigorPower", CardPriorityLevel.Offensive },
        { "AccuracyPower", CardPriorityLevel.Offensive },
        { "RagePower", CardPriorityLevel.Offensive }
    };

    private static readonly HashSet<string> DebuffPowerNames =
    [
        "VulnerablePower",
        "WeakPower",
        "PoisonPower",
        "BurnPower",
        "DebilitatePower",
        "EntanglePower",
        "SlowPower",
        "DoomPower"
    ];

    private static readonly HashSet<string> BuffPowerNames =
    [
        "StrengthPower",
        "DexterityPower",
        "FocusPower",
        "ArtifactPower",
        "BufferPower",
        "IntangiblePower",
        "RegenPower",
        "ThornsPower",
        "VigorPower",
        "AccuracyPower",
        "RagePower",
        "PlatingPower"
    ];

    private static readonly Dictionary<string, int> BuffPriorityScores = new()
    {
        { "StrengthPower", 150 },
        { "DexterityPower", 140 },
        { "FocusPower", 130 },
        { "ArtifactPower", 160 },
        { "BufferPower", 155 },
        { "IntangiblePower", 170 },
        { "RegenPower", 100 },
        { "ThornsPower", 90 },
        { "VigorPower", 85 },
        { "AccuracyPower", 80 },
        { "RagePower", 95 },
        { "PlatingPower", 105 }
    };

    private enum SpecialCardType
    {
        None,
        Dismantle,
        Spite,
        BubbleBubble,
        GoForTheEyes,
        FollowThrough
    }

    private static readonly Dictionary<string, SpecialCardType> SpecialCardTypes = new()
    {
        { "Dismantle", SpecialCardType.Dismantle },
        { "Spite", SpecialCardType.Spite },
        { "BubbleBubble", SpecialCardType.BubbleBubble },
        { "GoForTheEyes", SpecialCardType.GoForTheEyes },
        { "FollowThrough", SpecialCardType.FollowThrough }
    };

    private sealed class EnemyCache
    {
        public Creature Enemy = null!;
        public int CurrentHp;
        public int MaxHp;
        public bool IntendsToAttack;
        public bool HasVulnerable;
        public bool HasPoison;
        public bool HasWeak;
        public bool HasDebilitate;
        public int ThreatScore;
        public int PoisonAmount;
        public int Block;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        MainFile.Logger.Info("Vakuu is taking over player turn!");
        await HandleVakuuTurn(choiceContext, player);
    }

    private async Task HandleVakuuTurn(PlayerChoiceContext choiceContext, Player player)
    {
        try
        {
            var hand = PileType.Hand.GetPile(player);
            var combatState = (CombatState?)player.Creature.CombatState;
            if (combatState == null)
            {
                MainFile.Logger.Warn("Vakuu: No combat state found!");
                return;
            }

            MainFile.Logger.Info($"Vakuu handles turn for player. Hand count: {hand.Cards.Count}");

            var gameState = AnalyzeGameState(player, combatState);
            var enemyCache = CacheEnemies(combatState);

            UseOptimalPotion(choiceContext, player, combatState, gameState, enemyCache);

            int cardsPlayed = 0;
            var attemptedCards = new HashSet<CardModel>();
            var cardScores = new Dictionary<CardModel, int>();
            var energyCostCache = new Dictionary<CardModel, int>();

            PreCalculateCardEnergyCosts(hand.Cards, energyCostCache);

            while (cardsPlayed < MaxCardsToPlay)
            {
                if (CombatManager.Instance.IsOverOrEnding)
                {
                    MainFile.Logger.Info("Vakuu: Combat is over or ending");
                    break;
                }

                var card = SelectBestCardOptimized(hand.Cards, player, combatState, gameState, enemyCache, attemptedCards, cardScores, energyCostCache);
                if (card == null)
                {
                    MainFile.Logger.Info($"Vakuu: No more playable cards. Cards played: {cardsPlayed}");
                    break;
                }

                attemptedCards.Add(card);

                var target = GetBestTarget(card, combatState, player, enemyCache);
                LogBloodStateStrategy(gameState.CurrentBloodState, gameState.BloodStateSmoothFactor, card);
                MainFile.Logger.Info($"Vakuu: Playing card {card.Title} (priority: {cardScores[card]}) with target {(target != null ? target.ModelId.ToString() : "null")}");

                var dynamicVars = card.DynamicVars;
                if (dynamicVars != null && dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
                {
                    gameState.BlockPlayedThisTurn += (int)blockVar.BaseValue;
                }

                await card.SpendResources();
                await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
                cardsPlayed++;
            }

            MainFile.Logger.Info($"Vakuu: Turn complete. Total cards played: {cardsPlayed}");

            PlayerCmd.EndTurn(player, canBackOut: false);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Vakuu: Critical error in turn handling: {ex.Message}\n{ex.StackTrace}");
            PlayerCmd.EndTurn(player, canBackOut: false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PreCalculateCardEnergyCosts(IReadOnlyList<CardModel> cards, Dictionary<CardModel, int> cache)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            cache[card] = card.EnergyCost?.GetResolved() ?? 999;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EnemyCache[] CacheEnemies(CombatState combatState)
    {
        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0)
        {
            return Array.Empty<EnemyCache>();
        }

        var cache = new EnemyCache[enemies.Count];
        
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null)
            {
                cache[i] = new EnemyCache
                {
                    Enemy = null!,
                    CurrentHp = 0,
                    MaxHp = 0,
                    IntendsToAttack = false,
                    HasVulnerable = false,
                    HasPoison = false,
                    HasWeak = false,
                    HasDebilitate = false,
                    ThreatScore = 0,
                    PoisonAmount = 0,
                    Block = 0
                };
                continue;
            }

            cache[i] = new EnemyCache
            {
                Enemy = enemy,
                CurrentHp = (int)enemy.CurrentHp,
                MaxHp = (int)enemy.MaxHp,
                IntendsToAttack = enemy.Monster?.IntendsToAttack == true,
                HasVulnerable = enemy.HasPower<VulnerablePower>(),
                HasPoison = enemy.HasPower<PoisonPower>(),
                HasWeak = enemy.HasPower<WeakPower>(),
                HasDebilitate = enemy.HasPower<DebilitatePower>(),
                ThreatScore = CalculateEnemyThreatFast(enemy),
                PoisonAmount = enemy.GetPower<PoisonPower>()?.Amount ?? 0,
                Block = enemy.Block
            };
        }
        
        return cache;
    }

    private CardModel? SelectBestCardOptimized(
        IReadOnlyList<CardModel> cards, 
        Player player, 
        CombatState combatState, 
        GameStateInfo gameState,
        EnemyCache[] enemyCache,
        HashSet<CardModel> attemptedCards,
        Dictionary<CardModel, int> cardScores,
        Dictionary<CardModel, int> energyCostCache)
    {
        CardModel? bestZeroCostCard = null;
        int bestZeroCostScore = int.MinValue;
        
        CardModel? bestFinisherCard = null;
        int bestFinisherScore = int.MinValue;
        
        CardModel? bestNormalCard = null;
        int bestNormalScore = int.MinValue;

        bool hasLowHpEnemy = CheckLowHpEnemyExistsFast(enemyCache, 20);

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (attemptedCards.Contains(card)) continue;
            if (!card.CanPlay()) continue;

            if (!ShouldUseHealthCostCard(card, player, combatState, gameState, enemyCache))
            {
                MainFile.Logger.Debug($"Vakuu: Skipping {card.Title} - health cost card not allowed now");
                attemptedCards.Add(card);
                continue;
            }

            if (!ShouldUseOneCostDrawCard(card, player, combatState, gameState, enemyCache))
            {
                MainFile.Logger.Debug($"Vakuu: Skipping {card.Title} - 1-cost draw card not optimal now");
                attemptedCards.Add(card);
                continue;
            }

            if (!cardScores.TryGetValue(card, out int score))
            {
                score = EvaluateCardWithBloodState(card, player, combatState, gameState, enemyCache);
                cardScores[card] = score;
            }

            if (!energyCostCache.TryGetValue(card, out int energyCost))
            {
                energyCost = card.EnergyCost?.GetResolved() ?? 999;
                energyCostCache[card] = energyCost;
            }

            if (energyCost == 0)
            {
                if (score > bestZeroCostScore)
                {
                    bestZeroCostScore = score;
                    bestZeroCostCard = card;
                }
            }
            else if (hasLowHpEnemy && IsFinisherCardFast(card, player, enemyCache))
            {
                if (score > bestFinisherScore)
                {
                    bestFinisherScore = score;
                    bestFinisherCard = card;
                }
            }
            else
            {
                if (score > bestNormalScore)
                {
                    bestNormalScore = score;
                    bestNormalCard = card;
                }
            }
        }

        if (bestZeroCostCard != null && bestZeroCostScore > ScoreWeights.ZeroCostThreshold)
        {
            MainFile.Logger.Debug($"Vakuu: Selected 0-cost card {bestZeroCostCard.Title} (score: {bestZeroCostScore})");
            return bestZeroCostCard;
        }

        if (bestFinisherCard != null && bestFinisherScore > ScoreWeights.FinisherThreshold)
        {
            MainFile.Logger.Debug($"Vakuu: Selected finisher card {bestFinisherCard.Title} (score: {bestFinisherScore})");
            return bestFinisherCard;
        }

        if (bestNormalCard != null)
        {
            MainFile.Logger.Debug($"Vakuu: Selected normal card {bestNormalCard.Title} (score: {bestNormalScore})");
            return bestNormalCard;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckLowHpEnemyExistsFast(EnemyCache[] enemyCache, int hpThreshold)
    {
        for (int i = 0; i < enemyCache.Length; i++)
        {
            if (enemyCache[i].CurrentHp <= hpThreshold)
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsFinisherCardFast(CardModel card, Player player, EnemyCache[] enemyCache)
    {
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return false;

        if (!dynamicVars.TryGetValue("Damage", out var damageVar) || damageVar == null) return false;

        int damage = (int)damageVar.BaseValue;
        
        for (int i = 0; i < enemyCache.Length; i++)
        {
            if (enemyCache[i].CurrentHp <= damage)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CalculateEnemyThreatFast(Creature enemy)
    {
        int threat = (int)enemy.CurrentHp;

        if (enemy.Monster?.IntendsToAttack == true)
        {
            threat += 50;
        }

        return threat;
    }

    private int EvaluateCardOptimized(CardModel card, Player player, CombatState combatState, GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        int score = 0;

        score += EvaluateZeroCostCards(card, player, gameState);
        score += EvaluateLowHpEnemyFinisher(card, player, combatState, gameState);

        var specialType = GetSpecialCardType(card);
        if (specialType != SpecialCardType.None)
        {
            score += EvaluateSpecialCard(card, player, combatState, gameState);
        }

        score += EvaluateDeckManipulationCards(card, player, combatState, gameState);
        score += EvaluateBuffValue(card, player, combatState, gameState);
        score += EvaluateDebuffValueOptimized(card, combatState, gameState, enemyCache);
        score += EvaluateBlockValue(card, gameState);
        score += EvaluateDamageValue(card, gameState);
        score += EvaluateUtilityValue(card, player, gameState);
        score += EvaluateEnergyEfficiency(card, player);
        score += EvaluateScenarioAdaptation(card, player, combatState, gameState);
        score += EvaluateComboPotential(card, player, combatState);
        score += EvaluateMultiplayerCards(card, player, combatState, gameState);
        score += EvaluateUpgradeCardEffect(card, player, combatState, gameState);

        return score;
    }

    private int EvaluateUpgradeCardEffect(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int score = 0;
        var cardName = card.GetType().Name;
        
        if (cardName == "Armaments" || cardName.Contains("Armaments"))
        {
            var dynamicVars = card.DynamicVars;
            if (dynamicVars != null && dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
            {
                int blockAmount = (int)blockVar.BaseValue;
                score += 80;
                
                int effectiveBlockNeeded = CalculateEffectiveBlockNeeded(gameState, blockAmount);
                if (effectiveBlockNeeded > 0)
                {
                    score += 60;
                }
                else
                {
                    score -= 40;
                }
            }
            
            var hand = PileType.Hand.GetPile(player);
            var upgradeableCards = hand.Cards
                .Where(c => c != card && c.CanPlay() && c.CurrentUpgradeLevel == 0)
                .ToList();
            
            if (upgradeableCards.Count > 0)
            {
                int bestCardValue = 0;
                CardModel? bestCard = null;
                
                foreach (var upgradeCard in upgradeableCards)
                {
                    int cardValue = EstimateCardUpgradeValue(upgradeCard, player, combatState, gameState);
                    if (cardValue > bestCardValue)
                    {
                        bestCardValue = cardValue;
                        bestCard = upgradeCard;
                    }
                }
                
                if (bestCard != null)
                {
                    score += bestCardValue;
                    MainFile.Logger.Debug($"Vakuu: Armaments can upgrade {bestCard.Title} (value: {bestCardValue})");
                }
                
                if (upgradeableCards.Count >= 2)
                {
                    score += 30;
                    MainFile.Logger.Debug($"Vakuu: Armaments has {upgradeableCards.Count} upgradeable cards");
                }
            }
            else
            {
                score -= 100;
                MainFile.Logger.Debug("Vakuu: Armaments has no upgradeable cards");
            }
            
            if (IsUpgradedCard(card))
            {
                score += 50;
                MainFile.Logger.Debug("Vakuu: Upgraded Armaments");
            }
        }
        
        return score;
    }

    private int EstimateCardUpgradeValue(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int baseValue = 0;
        var dynamicVars = card.DynamicVars;
        
        if (dynamicVars != null)
        {
            if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
            {
                int damage = (int)damageVar.BaseValue;
                baseValue += 100 + (damage * 5);
                
                if (card.Tags.Contains(CardTag.Strike))
                {
                    baseValue += 50;
                }
            }
            
            if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
            {
                int block = (int)blockVar.BaseValue;
                int effectiveBlock = CalculateEffectiveBlockNeeded(gameState, block);
                if (effectiveBlock > 0)
                {
                    baseValue += 80 + (effectiveBlock * 4);
                }
            }
            
            if (dynamicVars.TryGetValue("Cards", out var cardsVar) && cardsVar != null)
            {
                baseValue += 120 + (cardsVar.IntValue * 30);
            }
            
            foreach (var kvp in dynamicVars)
            {
                if (kvp.Key.EndsWith("Power") && BuffPowerNames.Contains(kvp.Key))
                {
                    if (BuffPriorityScores.TryGetValue(kvp.Key, out int buffScore))
                    {
                        baseValue += buffScore + 40;
                    }
                }
            }
        }
        
        if (card.Type == CardType.Attack)
        {
            baseValue += 40;
        }
        else if (card.Type == CardType.Skill)
        {
            baseValue += 30;
        }
        
        int energyCost = card.EnergyCost?.GetResolved() ?? 0;
        if (energyCost >= 2)
        {
            baseValue += 50;
        }
        
        return baseValue;
    }

    private int EvaluateDebuffValueOptimized(CardModel card, CombatState combatState, GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        if (enemyCache == null || enemyCache.Length == 0)
        {
            return 0;
        }

        int score = 0;
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return 0;

        var player = combatState.PlayerCreatures?.FirstOrDefault()?.Player;
        if (player == null) return 0;

        foreach (var kvp in dynamicVars)
        {
            if (!DebuffPowerNames.Contains(kvp.Key)) continue;

            int enemiesWithoutDebuff = 0;
            int attackingEnemiesWithoutDebuff = 0;
            int totalEnemies = 0;
            int highPriorityTargets = 0;
            int lowHpEnemies = 0;

            for (int i = 0; i < enemyCache.Length; i++)
            {
                var ec = enemyCache[i];
                if (ec.Enemy == null) continue;

                totalEnemies++;

                bool hasDebuff = kvp.Key switch
                {
                    "VulnerablePower" => ec.HasVulnerable,
                    "WeakPower" => ec.HasWeak,
                    "DebilitatePower" => ec.HasDebilitate,
                    "PoisonPower" => ec.HasPoison,
                    _ => false
                };

                if (!hasDebuff)
                {
                    enemiesWithoutDebuff++;
                    
                    if (ec.IntendsToAttack)
                    {
                        attackingEnemiesWithoutDebuff++;
                    }

                    if (ec.CurrentHp <= 20)
                    {
                        lowHpEnemies++;
                    }

                    if (ec.IntendsToAttack && ec.ThreatScore > 15)
                    {
                        highPriorityTargets++;
                    }
                }
            }

            if (enemiesWithoutDebuff > 0)
            {
                score += 120 + (enemiesWithoutDebuff * 20);

                if (kvp.Key == "VulnerablePower")
                {
                    score += 40;
                    
                    if (highPriorityTargets > 0)
                    {
                        score += highPriorityTargets * 35;
                        MainFile.Logger.Debug($"Vakuu: Vulnerable on {highPriorityTargets} high-damage enemies");
                    }

                    if (gameState.EnemiesWithVulnerable > 0)
                    {
                        score += 50;
                        
                        var hand = PileType.Hand.GetPile(player);
                        if (hand != null)
                        {
                            for (int i = 0; i < hand.Cards.Count; i++)
                            {
                                if (GetSpecialCardType(hand.Cards[i]) == SpecialCardType.Dismantle)
                                {
                                    score += 60;
                                    MainFile.Logger.Debug($"Vakuu: Vulnerable + Dismantle combo detected");
                                    break;
                                }
                            }
                        }
                    }

                    var damageCards = CountDamageCardsInHand(player);
                    if (damageCards > 0)
                    {
                        score += damageCards * 15;
                        MainFile.Logger.Debug($"Vakuu: Vulnerable will boost {damageCards} damage cards");
                    }
                }

                if (kvp.Key == "WeakPower")
                {
                    score += 50;

                    if (attackingEnemiesWithoutDebuff > 0)
                    {
                        score += attackingEnemiesWithoutDebuff * 40;
                        MainFile.Logger.Debug($"Vakuu: Weak on {attackingEnemiesWithoutDebuff} attacking enemies");
                    }

                    if (gameState.IsInDanger || gameState.PlayerHpPercent < 50)
                    {
                        score += 60;
                        MainFile.Logger.Debug($"Vakuu: Weak is crucial for survival (HP: {gameState.PlayerHpPercent}%)");
                    }

                    int totalIncomingDamage = 0;
                    for (int i = 0; i < enemyCache.Length; i++)
                    {
                        var ec = enemyCache[i];
                        if (ec.Enemy != null && ec.IntendsToAttack && !ec.HasWeak)
                        {
                            totalIncomingDamage += ec.ThreatScore;
                        }
                    }
                    
                    if (totalIncomingDamage > 30)
                    {
                        score += 50;
                        MainFile.Logger.Debug($"Vakuu: Weak can reduce {totalIncomingDamage} incoming damage");
                    }
                }

                if (kvp.Key == "DebilitatePower")
                {
                    score += 60;

                    if (attackingEnemiesWithoutDebuff > 0)
                    {
                        score += attackingEnemiesWithoutDebuff * 45;
                        MainFile.Logger.Debug($"Vakuu: Debilitate on {attackingEnemiesWithoutDebuff} attacking enemies (double debuff!)");
                    }

                    if (highPriorityTargets > 0)
                    {
                        score += highPriorityTargets * 40;
                        MainFile.Logger.Debug($"Vakuu: Debilitate on {highPriorityTargets} high-damage enemies");
                    }

                    if (gameState.IsInDanger || gameState.PlayerHpPercent < 50)
                    {
                        score += 70;
                        MainFile.Logger.Debug($"Vakuu: Debilitate is crucial for survival (HP: {gameState.PlayerHpPercent}%)");
                    }

                    int totalIncomingDamage = 0;
                    for (int i = 0; i < enemyCache.Length; i++)
                    {
                        var ec = enemyCache[i];
                        if (ec.Enemy != null && ec.IntendsToAttack && !ec.HasDebilitate)
                        {
                            totalIncomingDamage += ec.ThreatScore;
                        }
                    }
                    
                    if (totalIncomingDamage > 30)
                    {
                        score += 60;
                        MainFile.Logger.Debug($"Vakuu: Debilitate can reduce {totalIncomingDamage} incoming damage (Weak + Vulnerable)");
                    }

                    var damageCards = CountDamageCardsInHand(player);
                    if (damageCards > 0)
                    {
                        score += damageCards * 20;
                        MainFile.Logger.Debug($"Vakuu: Debilitate will boost {damageCards} damage cards (Vulnerable effect)");
                    }
                }

                if (kvp.Key == "PoisonPower")
                {
                    score += 25;
                    
                    if (lowHpEnemies > 0)
                    {
                        score += lowHpEnemies * 50;
                        MainFile.Logger.Debug($"Vakuu: Poison can finish {lowHpEnemies} low HP enemies");
                    }

                    int totalEnemyMaxHp = 0;
                    for (int i = 0; i < enemyCache.Length; i++)
                    {
                        var ec = enemyCache[i];
                        if (ec.Enemy != null && !ec.HasPoison)
                        {
                            totalEnemyMaxHp += (int)ec.Enemy.MaxHp;
                        }
                    }
                    
                    if (totalEnemyMaxHp > 60)
                    {
                        score += 40;
                        MainFile.Logger.Debug($"Vakuu: Poison effective against high HP enemies ({totalEnemyMaxHp})");
                    }
                    
                    var hand = PileType.Hand.GetPile(player);
                    if (hand != null)
                    {
                        for (int i = 0; i < hand.Cards.Count; i++)
                        {
                            if (GetSpecialCardType(hand.Cards[i]) == SpecialCardType.BubbleBubble)
                            {
                                score += 50;
                                MainFile.Logger.Debug($"Vakuu: Poison + BubbleBubble combo detected");
                                break;
                            }
                        }
                    }
                }

                if (gameState.EnemyIntendsAttack)
                {
                    score += 30;
                }

                if (attackingEnemiesWithoutDebuff > 0)
                {
                    score += 40 + (attackingEnemiesWithoutDebuff * 25);
                    MainFile.Logger.Debug($"Vakuu: Debuff {kvp.Key} can affect {attackingEnemiesWithoutDebuff} attacking enemies");
                }

                if (IsUpgradedCard(card))
                {
                    score += 40;
                    MainFile.Logger.Debug($"Vakuu: Upgraded debuff card {card.Title}, debuff: {kvp.Key}");
                }
            }
        }

        return score;
    }

    private int CountDamageCardsInHand(Player player)
    {
        int count = 0;
        var hand = PileType.Hand.GetPile(player);
        if (hand == null) return 0;

        for (int i = 0; i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            if (card.DynamicVars != null && card.DynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
            {
                if (damageVar.BaseValue > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountRemainingAttackCardsInHand(Player player, CardModel? currentCard, int energyCostOverride = -1)
    {
        int count = 0;
        var hand = PileType.Hand.GetPile(player);
        if (hand == null) return 0;

        int energyCost = energyCostOverride >= 0 ? energyCostOverride : (currentCard?.EnergyCost?.GetResolved() ?? 0);
        int remainingEnergy = (player.PlayerCombatState?.Energy ?? 0) - energyCost;

        for (int i = 0; i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            if (card == currentCard) continue;
            if (card.Type != CardType.Attack) continue;
            
            var cardEnergyCost = card.EnergyCost?.GetResolved() ?? 999;
            if (cardEnergyCost <= remainingEnergy)
            {
                count++;
            }
        }

        return count;
    }

    private GameStateInfo AnalyzeGameState(Player player, CombatState combatState)
    {
        var info = new GameStateInfo
        {
            PlayerCurrentHp = (int)player.Creature.CurrentHp,
            PlayerMaxHp = (int)player.Creature.MaxHp,
            PlayerCurrentEnergy = player.PlayerCombatState?.Energy ?? 0,
            PlayerBlock = (int)player.Creature.Block,
            IncomingDamage = CalculateIncomingDamage(combatState),
            EnemyIntendsAttack = CheckEnemyAttackIntent(combatState),
            EnemyCount = combatState.HittableEnemies.Count,
            HandSize = PileType.Hand.GetPile(player).Cards.Count
        };

        info.PlayerHpPercent = info.PlayerMaxHp > 0 ? (info.PlayerCurrentHp * 100 / info.PlayerMaxHp) : 0;
        info.IsInDanger = info.PlayerHpPercent < 30 || (info.IncomingDamage > info.PlayerCurrentHp - info.PlayerBlock);
        info.NeedsBlock = info.EnemyIntendsAttack && info.IncomingDamage > info.PlayerBlock;

        info.LostHpThisTurn = LostHpThisTurn(player.Creature, combatState);
        info.WasLastCardSkill = WasLastCardPlayedSkill(player, combatState);

        float hpPercentDecimal = info.PlayerHpPercent / 100.0f;
        info.CurrentBloodState = DetermineBloodState(hpPercentDecimal);
        info.BloodStateSmoothFactor = CalculateBloodStateSmoothFactor(hpPercentDecimal, info.CurrentBloodState);

        var enemies = combatState.HittableEnemies;
        info.EnemiesWithVulnerable = 0;
        info.EnemiesWithPoison = 0;
        info.EnemiesIntendingAttack = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy.HasPower<VulnerablePower>())
            {
                info.EnemiesWithVulnerable++;
            }
            if (enemy.HasPower<PoisonPower>())
            {
                info.EnemiesWithPoison++;
            }
            if (enemy.Monster?.IntendsToAttack == true)
            {
                info.EnemiesIntendingAttack++;
            }
        }

        return info;
    }

    private BloodHealthState DetermineBloodState(float hpPercentDecimal)
    {
        if (hpPercentDecimal <= LowHpThreshold)
        {
            return BloodHealthState.Low;
        }
        else if (hpPercentDecimal <= MediumHpThreshold)
        {
            return BloodHealthState.Medium;
        }
        else
        {
            return BloodHealthState.High;
        }
    }

    private float CalculateBloodStateSmoothFactor(float hpPercentDecimal, BloodHealthState currentState)
    {
        float buffer = LowHpBuffer;
        
        return currentState switch
        {
            BloodHealthState.Low => Clamp01((hpPercentDecimal - (LowHpThreshold - buffer)) / buffer),
            BloodHealthState.Medium => Clamp01((hpPercentDecimal - LowHpThreshold) / (MediumHpThreshold - LowHpThreshold)),
            BloodHealthState.High => Clamp01((hpPercentDecimal - MediumHpThreshold) / (1.0f - MediumHpThreshold)),
            _ => 0.5f
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Clamp01(float value)
    {
        if (value < 0.0f) return 0.0f;
        if (value > 1.0f) return 1.0f;
        return value;
    }



    private int EvaluateBuffValue(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int score = 0;
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return 0;

        var selfTargetTypes = new[] { TargetType.Self, TargetType.AllAllies, TargetType.AnyAlly, TargetType.AnyPlayer };
        bool canTargetSelf = selfTargetTypes.Contains(card.TargetType);

        if (!canTargetSelf) return 0;

        foreach (var kvp in dynamicVars)
        {
            if (!BuffPowerNames.Contains(kvp.Key)) continue;

            if (BuffPriorityScores.TryGetValue(kvp.Key, out int baseScore))
            {
                score += baseScore;

                if (kvp.Key == "StrengthPower")
                {
                    score += 40 + (gameState.EnemyCount * 15);
                    
                    int remainingAttackCards = CountRemainingAttackCardsInHand(player, card);
                    if (remainingAttackCards > 0)
                    {
                        score += remainingAttackCards * 25;
                        MainFile.Logger.Debug($"Vakuu: StrengthPower will boost {remainingAttackCards} remaining attack cards");
                    }
                    
                    if (gameState.PlayerCurrentEnergy >= 2)
                    {
                        score += 30;
                        MainFile.Logger.Debug($"Vakuu: Have enough energy to use StrengthPower");
                    }
                }

                if (kvp.Key == "DexterityPower" && gameState.NeedsBlock)
                {
                    score += 50;
                }

                if (kvp.Key == "BufferPower" && gameState.IsInDanger)
                {
                    score += 60;
                }

                if (kvp.Key == "IntangiblePower" && gameState.PlayerHpPercent < 40)
                {
                    score += 80;
                }

                if (kvp.Key == "RegenPower" && gameState.PlayerHpPercent < 50)
                {
                    score += 45;
                }

                if (kvp.Key == "PlatingPower" && gameState.PlayerHpPercent < 70)
                {
                    score += 35;
                }

                if (!PlayerHasBuff(player.Creature, kvp.Key))
                {
                    score += 30;
                }
                
                if (IsUpgradedCard(card))
                {
                    score += 45;
                    MainFile.Logger.Debug($"Vakuu: Upgraded buff card {card.Title}, buff: {kvp.Key}");
                }
            }
        }

        return score;
    }

    private bool PlayerHasBuff(Creature player, string powerName)
    {
        return powerName switch
        {
            "StrengthPower" => player.HasPower<StrengthPower>(),
            "DexterityPower" => player.HasPower<DexterityPower>(),
            "FocusPower" => player.HasPower<FocusPower>(),
            "ArtifactPower" => player.HasPower<ArtifactPower>(),
            "BufferPower" => player.HasPower<BufferPower>(),
            "IntangiblePower" => player.HasPower<IntangiblePower>(),
            "RegenPower" => player.HasPower<RegenPower>(),
            "ThornsPower" => player.HasPower<ThornsPower>(),
            "VigorPower" => player.HasPower<VigorPower>(),
            "AccuracyPower" => player.HasPower<AccuracyPower>(),
            "RagePower" => player.HasPower<RagePower>(),
            "PlatingPower" => player.HasPower<PlatingPower>(),
            _ => false
        };
    }

    private int EvaluateBlockValue(CardModel card, GameStateInfo gameState)
    {
        int score = 0;

        if (card.Tags.Contains(CardTag.Defend))
        {
            score += ScoreWeights.BlockBase + 10;
        }

        var dynamicVars = card.DynamicVars;
        if (dynamicVars != null && dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
        {
            int blockAmount = (int)blockVar.BaseValue;
            int effectiveBlockNeeded = CalculateEffectiveBlockNeeded(gameState, blockAmount);
            
            if (effectiveBlockNeeded <= 0)
            {
                int overflowPenalty = CalculateBlockOverflowPenalty(gameState, blockAmount);
                score -= overflowPenalty;
                
                if (overflowPenalty > 50)
                {
                    MainFile.Logger.Debug($"Vakuu: Block card {card.Title} has severe overflow (block:{blockAmount}, needed:0), score penalty: -{overflowPenalty}");
                    return score;
                }
            }
            
            score += ScoreWeights.BlockBase;

            if (gameState.NeedsBlock)
            {
                score += 60;
            }

            if (gameState.IsInDanger)
            {
                score += 40;
            }
            
            if (effectiveBlockNeeded < blockAmount)
            {
                int overflowAmount = blockAmount - effectiveBlockNeeded;
                float overflowRatio = overflowAmount / (float)blockAmount;
                
                if (overflowRatio > 0.5f)
                {
                    score -= 40;
                    MainFile.Logger.Debug($"Vakuu: Block card {card.Title} has {overflowRatio:P0} overflow");
                }
                else if (overflowRatio > 0.3f)
                {
                    score -= 20;
                }
            }
            
            if (IsUpgradedCard(card))
            {
                score += 40;
                MainFile.Logger.Debug($"Vakuu: Upgraded block card {card.Title}, block: {blockAmount}");
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CalculateBlockOverflowPenalty(GameStateInfo gameState, int blockAmount)
    {
        int incomingDamage = gameState.IncomingDamage;
        int currentBlock = gameState.PlayerBlock + gameState.BlockPlayedThisTurn;
        int totalBlockAfterPlay = currentBlock + blockAmount;
        
        if (incomingDamage <= 0)
        {
            int maxUsefulBlock = gameState.PlayerMaxHp / 3;
            if (totalBlockAfterPlay > maxUsefulBlock * 2)
            {
                return 80;
            }
            else if (totalBlockAfterPlay > maxUsefulBlock)
            {
                return 40;
            }
            return 20;
        }
        
        int blockDeficit = incomingDamage - currentBlock;
        if (blockDeficit <= 0)
        {
            int excessBlock = totalBlockAfterPlay - incomingDamage;
            float overflowRatio = excessBlock / (float)blockAmount;
            
            if (overflowRatio > 0.8f)
            {
                return 70;
            }
            else if (overflowRatio > 0.5f)
            {
                return 50;
            }
            else if (overflowRatio > 0.3f)
            {
                return 30;
            }
        }
        
        return 10;
    }

    private int EvaluateDamageValue(CardModel card, GameStateInfo gameState)
    {
        int score = 0;
        var dynamicVars = card.DynamicVars;

        if (dynamicVars != null && dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
        {
            int damage = (int)damageVar.BaseValue;
            score += ScoreWeights.DamageBase;
            
            if (IsUpgradedCard(card))
            {
                score += 50;
                MainFile.Logger.Debug($"Vakuu: Upgraded damage card {card.Title}, damage: {damage}");
            }

            if (!gameState.EnemyIntendsAttack)
            {
                score += 20;
            }
        }

        if (card.Tags.Contains(CardTag.Strike))
        {
            score += 40;
        }

        return score;
    }

    private int EvaluateUtilityValue(CardModel card, Player player, GameStateInfo gameState)
    {
        int score = 0;
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return 0;

        if (dynamicVars.TryGetValue("Energy", out var energyVar) && energyVar != null)
        {
            score += ScoreWeights.HealBase;

            int currentEnergy = player.PlayerCombatState?.Energy ?? 0;
            if (currentEnergy <= 1)
            {
                score += 50;
            }
            else if (currentEnergy <= 2)
            {
                score += 30;
            }
        }

        if (dynamicVars.TryGetValue("Cards", out var cardsVar) && cardsVar != null)
        {
            score += EvaluateCardDrawValue(card, player, gameState);
        }

        if (dynamicVars.TryGetValue("Heal", out var healVar) && healVar != null)
        {
            if (gameState.PlayerHpPercent < 50)
            {
                score += ScoreWeights.HealBase;
            }
            else if (gameState.PlayerHpPercent < 70)
            {
                score += 50;
            }
            else
            {
                score += 20;
            }
        }

        return score;
    }

    private int EvaluateCardDrawValue(CardModel card, Player player, GameStateInfo gameState)
    {
        int score = 0;
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return 0;

        if (!dynamicVars.TryGetValue("Cards", out var cardsVar)) return 0;

        int cardsToDraw = cardsVar.IntValue;
        int currentEnergy = player.PlayerCombatState?.Energy ?? 0;
        int energyAfterPlay = currentEnergy - (card.EnergyCost?.GetResolved() ?? 0);

        if (gameState.HandSize <= 3)
        {
            score += 100 + (cardsToDraw * 25);
        }
        else if (gameState.HandSize <= 5)
        {
            score += 80 + (cardsToDraw * 20);
        }
        else if (gameState.HandSize <= 7)
        {
            score += 50 + (cardsToDraw * 10);
        }
        else
        {
            score += 20;
        }

        if (energyAfterPlay >= 1)
        {
            score += 30;
        }
        
        if (IsUpgradedCard(card))
        {
            score += 35;
            MainFile.Logger.Debug($"Vakuu: Upgraded draw card {card.Title}, cards: {cardsToDraw}");
        }

        var hand = PileType.Hand.GetPile(player);
        int playableCardsInHand = 0;
        for (int i = 0; i < hand.Cards.Count; i++)
        {
            if (hand.Cards[i].CanPlay())
            {
                playableCardsInHand++;
            }
        }

        if (playableCardsInHand <= 1 && cardsToDraw > 0)
        {
            score += 60;
        }

        var enemies = player.Creature.CombatState?.HittableEnemies;
        if (enemies != null && enemies.Count > 0)
        {
            bool enemyLowHp = false;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].CurrentHp <= 20)
                {
                    enemyLowHp = true;
                    break;
                }
            }

            if (enemyLowHp)
            {
                score -= 30;
            }
        }

        return score;
    }

    private int EvaluateEnergyEfficiency(CardModel card, Player player)
    {
        int score = 0;
        var energyCost = card.EnergyCost;
        if (energyCost == null) return 0;

        int cost = energyCost.GetResolved();
        int currentEnergy = player.PlayerCombatState?.Energy ?? 0;
        bool isXCostCard = energyCost.CostsX;

        if (isXCostCard)
        {
            if (currentEnergy >= 3)
            {
                score += 80;
            }
            else if (currentEnergy >= 1)
            {
                score += 50;
            }
            else
            {
                score -= 30;
            }

            var dynamicVars = card.DynamicVars;
            if (dynamicVars != null)
            {
                if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null && damageVar.BaseValue > 0)
                {
                    int expectedDamage = (int)(damageVar.BaseValue * currentEnergy);
                    score += 60;

                    var enemies = player.Creature?.CombatState?.HittableEnemies;
                    if (enemies != null && enemies.Count > 0)
                    {
                        int totalEnemyHp = 0;
                        for (int i = 0; i < enemies.Count; i++)
                        {
                            totalEnemyHp += (int)enemies[i].CurrentHp;
                        }
                        
                        if (expectedDamage >= totalEnemyHp * 0.6)
                        {
                            score += 50;
                        }
                    }
                }
                if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null && blockVar.BaseValue > 0)
                {
                    score += 60;
                }
                if (dynamicVars.ContainsKey("Cards"))
                {
                    score += 40;
                }
            }
        }
        else if (cost >= 3)
        {
            score += 50;

            var dynamicVars = card.DynamicVars;
            if (dynamicVars != null)
            {
                int totalImpact = 0;
                
                if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
                {
                    totalImpact += (int)damageVar.BaseValue;
                }
                
                if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
                {
                    totalImpact += (int)blockVar.BaseValue;
                }

                foreach (var kvp in dynamicVars)
                {
                    if (kvp.Value is DynamicVar powerVar && kvp.Key.EndsWith("Power"))
                    {
                        totalImpact += (int)powerVar.BaseValue * 20;
                    }
                }

                decimal efficiency = cost > 0 ? totalImpact / cost : totalImpact;
                
                if (efficiency >= 8)
                {
                    score += 100;
                }
                else if (efficiency >= 6)
                {
                    score += 70;
                }
                else if (efficiency >= 4)
                {
                    score += 40;
                }
            }

            if (card.Type == CardType.Attack)
            {
                score += 30;
            }
            else if (card.Type == CardType.Skill)
            {
                score += 25;
            }
        }
        else if (cost == 0)
        {
            score += 30;
        }
        else if (cost < 0)
        {
            score += 35;
        }
        else if (cost <= currentEnergy)
        {
            score += 10;
        }

        return score;
    }

    private bool CheckEnemyAttackIntent(CombatState combatState)
    {
        var enemies = combatState.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var creature = enemies[i];
            if (creature == null || !creature.IsAlive) continue;

            var monster = creature.Monster;
            if (monster != null && monster.IntendsToAttack)
            {
                return true;
            }
        }
        return false;
    }

    private int CalculateIncomingDamage(CombatState combatState)
    {
        int totalDamage = 0;
        var enemies = combatState.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            var creature = enemies[i];
            if (creature == null || !creature.IsAlive) continue;

            var monster = creature.Monster;
            if (monster != null)
            {
                var intents = monster.NextMove.Intents;
                foreach (var intent in intents)
                {
                    if (intent is AttackIntent attackIntent)
                    {
                        var targets = combatState.PlayerCreatures;
                        totalDamage += attackIntent.GetTotalDamage(targets, creature);
                    }
                }
            }
        }
        return totalDamage;
    }

    private bool EnemyHasDebuff(Creature enemy, string powerName)
    {
        return powerName switch
        {
            "VulnerablePower" => enemy.HasPower<VulnerablePower>(),
            "WeakPower" => enemy.HasPower<WeakPower>(),
            "PoisonPower" => enemy.HasPower<PoisonPower>(),
            "DebilitatePower" => enemy.HasPower<DebilitatePower>(),
            _ => false
        };
    }

    private Creature? GetBestTarget(CardModel card, CombatState combatState, Player player, EnemyCache[] enemyCache)
    {
        if (enemyCache.Length == 0) return null;

        var specialType = GetSpecialCardType(card);
        if (specialType != SpecialCardType.None)
        {
            var specialTarget = SelectBestTargetForSpecialCard(card, specialType, enemyCache, combatState, player);
            if (specialTarget != null)
            {
                MainFile.Logger.Debug($"Vakuu: Selected special target for {specialType}: {specialTarget.ModelId}");
                return specialTarget;
            }
        }

        return card.TargetType switch
        {
            TargetType.AnyEnemy => SelectBestEnemyTargetOptimized(card, enemyCache),
            TargetType.AnyAlly => SelectBestAllyTarget(combatState, player.Creature),
            TargetType.AnyPlayer => player.Creature,
            TargetType.Self => player.Creature,
            TargetType.AllEnemies => SelectBestEnemyTargetOptimized(card, enemyCache) ?? enemyCache[0].Enemy,
            TargetType.AllAllies => player.Creature,
            _ => null
        };
    }

    private Creature? SelectBestEnemyTargetOptimized(CardModel card, EnemyCache[] enemyCache)
    {
        if (HasDebuffCard(card))
        {
            Creature? bestTarget = null;
            int highestThreat = -1;

            for (int i = 0; i < enemyCache.Length; i++)
            {
                var ec = enemyCache[i];
                if (ec.HasVulnerable || ec.HasWeak || ec.HasPoison) continue;

                if (ec.ThreatScore > highestThreat)
                {
                    highestThreat = ec.ThreatScore;
                    bestTarget = ec.Enemy;
                }
            }

            if (bestTarget != null) return bestTarget;
        }

        {
            Creature? bestTarget = null;
            int lowestHp = int.MaxValue;

            for (int i = 0; i < enemyCache.Length; i++)
            {
                var ec = enemyCache[i];
                if (ec.CurrentHp < lowestHp)
                {
                    lowestHp = ec.CurrentHp;
                    bestTarget = ec.Enemy;
                }
            }

            return bestTarget;
        }
    }



    private bool HasDebuffCard(CardModel card)
    {
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return false;

        foreach (var kvp in dynamicVars)
        {
            if (DebuffPowerNames.Contains(kvp.Key))
            {
                return true;
            }
        }
        return false;
    }

    private bool EnemyHasAnyDebuff(Creature enemy, CardModel card)
    {
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return false;

        foreach (var kvp in dynamicVars)
        {
            if (DebuffPowerNames.Contains(kvp.Key) && EnemyHasDebuff(enemy, kvp.Key))
            {
                return true;
            }
        }
        return false;
    }

    private Creature? SelectBestAllyTarget(CombatState combatState, Creature owner)
    {
        Creature? bestTarget = null;
        int lowestHpPercent = 100;

        var allies = combatState.Allies;
        for (int i = 0; i < allies.Count; i++)
        {
            var ally = allies[i];
            if (ally == null || !ally.IsAlive || !ally.IsPlayer || ally == owner) continue;

            int hpPercent = (int)(ally.CurrentHp * 100 / ally.MaxHp);
            if (hpPercent < lowestHpPercent)
            {
                lowestHpPercent = hpPercent;
                bestTarget = ally;
            }
        }

        return bestTarget ?? owner;
    }

    protected override void AfterRunCreated(RunState runState)
    {
        MainFile.Logger.Info("Vakuu Tower modifier created");
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info("Vakuu Tower modifier loaded");
    }

    public static VakuuTowerModifier? GetVakuuTowerModifier(RunState runState)
    {
        foreach (var modifier in runState.Modifiers)
        {
            if (modifier is VakuuTowerModifier vakuuModifier)
            {
                return vakuuModifier;
            }
        }
        return null;
    }

    public static bool IsVakuuTowerMode(RunState runState)
    {
        return GetVakuuTowerModifier(runState) != null;
    }

    private class GameStateInfo
    {
        public int PlayerCurrentHp { get; set; }
        public int PlayerMaxHp { get; set; }
        public int PlayerHpPercent { get; set; }
        public int PlayerCurrentEnergy { get; set; }
        public int PlayerBlock { get; set; }
        public int IncomingDamage { get; set; }
        public bool EnemyIntendsAttack { get; set; }
        public int EnemyCount { get; set; }
        public int HandSize { get; set; }
        public bool IsInDanger { get; set; }
        public bool NeedsBlock { get; set; }
        public bool LostHpThisTurn { get; set; }
        public bool WasLastCardSkill { get; set; }
        public int EnemiesWithVulnerable { get; set; }
        public int EnemiesWithPoison { get; set; }
        public int EnemiesIntendingAttack { get; set; }
        public BloodHealthState CurrentBloodState { get; set; }
        public float BloodStateSmoothFactor { get; set; }
        public int BlockPlayedThisTurn { get; set; }
    }

    private SpecialCardType GetSpecialCardType(CardModel card)
    {
        var cardName = card.GetType().Name;
        return SpecialCardTypes.TryGetValue(cardName, out var type) ? type : SpecialCardType.None;
    }

    private bool CheckSpecialCardCondition(SpecialCardType cardType, Creature? target, Player player, CombatState combatState)
    {
        return cardType switch
        {
            SpecialCardType.Dismantle => target != null && target.HasPower<VulnerablePower>(),
            SpecialCardType.Spite => LostHpThisTurn(player.Creature, combatState),
            SpecialCardType.BubbleBubble => target != null && target.HasPower<PoisonPower>(),
            SpecialCardType.GoForTheEyes => target != null && target.Monster?.IntendsToAttack == true,
            SpecialCardType.FollowThrough => WasLastCardPlayedSkill(player, combatState),
            _ => false
        };
    }

    private bool LostHpThisTurn(Creature creature, CombatState combatState)
    {
        return CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(e => e.HappenedThisTurn(combatState) && 
                      e.Receiver == creature && 
                      e.Result.UnblockedDamage > 0);
    }

    private bool WasLastCardPlayedSkill(Player player, CombatState combatState)
    {
        var lastCardPlay = CombatManager.Instance.History.CardPlaysStarted
            .LastOrDefault(e => e.CardPlay.Card.Owner == player && 
                               e.HappenedThisTurn(combatState) && 
                               e.CardPlay.Card != null);

        return lastCardPlay?.CardPlay.Card?.Type == CardType.Skill;
    }

    private int EvaluateSpecialCard(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        var specialType = GetSpecialCardType(card);
        if (specialType == SpecialCardType.None) return 0;

        int score = 0;
        var enemies = combatState.HittableEnemies;

        switch (specialType)
        {
            case SpecialCardType.Dismantle:
                if (gameState.EnemiesWithVulnerable > 0)
                {
                    score += 200;
                    score += gameState.EnemiesWithVulnerable * 50;
                    MainFile.Logger.Debug($"Vakuu: Dismantle bonus - {gameState.EnemiesWithVulnerable} enemies with Vulnerable");
                }
                else
                {
                    var dynamicVars = card.DynamicVars;
                    if (dynamicVars != null && dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
                    {
                        var damageValue = damageVar.BaseValue;
                        if (damageValue > 0)
                        {
                            score += 60;
                        }
                    }
                }
                if (IsUpgradedCard(card))
                {
                    score += 50;
                    MainFile.Logger.Debug($"Vakuu: Upgraded special card {card.Title} (Dismantle)");
                }
                break;

            case SpecialCardType.Spite:
                if (gameState.LostHpThisTurn)
                {
                    score += 250;
                    MainFile.Logger.Debug("Vakuu: Spite bonus - player lost HP this turn");
                }
                else
                {
                    score += 40;
                }
                if (IsUpgradedCard(card))
                {
                    score += 50;
                    MainFile.Logger.Debug($"Vakuu: Upgraded special card {card.Title} (Spite)");
                }
                break;

            case SpecialCardType.BubbleBubble:
                if (gameState.EnemiesWithPoison > 0)
                {
                    score += 180;
                    score += gameState.EnemiesWithPoison * 40;
                    MainFile.Logger.Debug($"Vakuu: BubbleBubble bonus - {gameState.EnemiesWithPoison} enemies with Poison");
                }
                else
                {
                    score += 70;
                }
                if (IsUpgradedCard(card))
                {
                    score += 50;
                    MainFile.Logger.Debug($"Vakuu: Upgraded special card {card.Title} (BubbleBubble)");
                }
                break;

            case SpecialCardType.GoForTheEyes:
                if (gameState.EnemiesIntendingAttack > 0)
                {
                    score += 160;
                    score += gameState.EnemiesIntendingAttack * 35;
                    MainFile.Logger.Debug($"Vakuu: GoForTheEyes bonus - {gameState.EnemiesIntendingAttack} enemies attacking");
                }
                else
                {
                    score += 50;
                }
                if (IsUpgradedCard(card))
                {
                    score += 50;
                    MainFile.Logger.Debug($"Vakuu: Upgraded special card {card.Title} (GoForTheEyes)");
                }
                break;

            case SpecialCardType.FollowThrough:
                if (gameState.WasLastCardSkill)
                {
                    score += 220;
                    MainFile.Logger.Debug("Vakuu: FollowThrough bonus - last card was skill");
                }
                else
                {
                    score += 45;
                }
                if (IsUpgradedCard(card))
                {
                    score += 50;
                    MainFile.Logger.Debug($"Vakuu: Upgraded special card {card.Title} (FollowThrough)");
                }
                break;
        }

        return score;
    }

    private Creature? SelectBestTargetForSpecialCard(CardModel card, SpecialCardType specialType, EnemyCache[] enemyCache, CombatState combatState, Player player)
    {
        if (enemyCache.Length == 0) return null;

        return specialType switch
        {
            SpecialCardType.Dismantle => SelectEnemyWithVulnerableOptimized(enemyCache) ?? SelectLowestHpEnemyOptimized(enemyCache),
            SpecialCardType.BubbleBubble => SelectEnemyWithPoisonOptimized(enemyCache) ?? SelectLowestHpEnemyOptimized(enemyCache),
            SpecialCardType.GoForTheEyes => SelectEnemyIntendingAttackOptimized(enemyCache) ?? SelectLowestHpEnemyOptimized(enemyCache),
            _ => SelectBestEnemyTargetOptimized(card, enemyCache)
        };
    }

    private Creature? SelectEnemyWithVulnerableOptimized(EnemyCache[] enemyCache)
    {
        Creature? bestTarget = null;
        int lowestHp = int.MaxValue;

        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.HasVulnerable)
            {
                if (ec.CurrentHp < lowestHp)
                {
                    lowestHp = ec.CurrentHp;
                    bestTarget = ec.Enemy;
                }
            }
        }

        return bestTarget;
    }

    private Creature? SelectEnemyWithPoisonOptimized(EnemyCache[] enemyCache)
    {
        Creature? bestTarget = null;
        int highestPoison = 0;

        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.HasPoison && ec.PoisonAmount > highestPoison)
            {
                highestPoison = ec.PoisonAmount;
                bestTarget = ec.Enemy;
            }
        }

        return bestTarget;
    }

    private Creature? SelectEnemyIntendingAttackOptimized(EnemyCache[] enemyCache)
    {
        Creature? bestTarget = null;
        int highestThreat = -1;

        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.IntendsToAttack && ec.ThreatScore > highestThreat)
            {
                highestThreat = ec.ThreatScore;
                bestTarget = ec.Enemy;
            }
        }

        return bestTarget;
    }

    private Creature? SelectLowestHpEnemyOptimized(EnemyCache[] enemyCache)
    {
        Creature? bestTarget = null;
        int lowestHp = int.MaxValue;

        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.CurrentHp < lowestHp)
            {
                lowestHp = ec.CurrentHp;
                bestTarget = ec.Enemy;
            }
        }

        return bestTarget;
    }

    private int EvaluateScenarioAdaptation(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int score = 0;

        if (gameState.IsInDanger)
        {
            if (card.Tags.Contains(CardTag.Defend))
            {
                int blockAmount = GetCardBlockAmount(card);
                int effectiveBlock = CalculateEffectiveBlockNeeded(gameState, blockAmount);
                if (effectiveBlock > 0)
                {
                    score += 100;
                }
            }
            if (card.DynamicVars?.TryGetValue("Heal", out var _) ?? false)
            {
                score += 80;
            }
        }

        if (gameState.PlayerHpPercent < 30)
        {
            if (card.DynamicVars?.TryGetValue("Heal", out var _) ?? false)
            {
                score += 120;
            }
            if (card.DynamicVars?.TryGetValue("Block", out var bv) ?? false)
            {
                int blockAmount = bv != null ? (int)bv.BaseValue : 0;
                int effectiveBlock = CalculateEffectiveBlockNeeded(gameState, blockAmount);
                if (effectiveBlock > 0)
                {
                    score += 90;
                }
            }
        }

        if (gameState.EnemyIntendsAttack && gameState.IncomingDamage > gameState.PlayerBlock + gameState.BlockPlayedThisTurn + 20)
        {
            if (card.Tags.Contains(CardTag.Defend))
            {
                int blockAmount = GetCardBlockAmount(card);
                int effectiveBlock = CalculateEffectiveBlockNeeded(gameState, blockAmount);
                if (effectiveBlock > 0)
                {
                    score += 110;
                }
            }
        }

        if (!gameState.EnemyIntendsAttack)
        {
            if (card.Tags.Contains(CardTag.Strike) || (card.DynamicVars?.TryGetValue("Damage", out var _) ?? false))
            {
                score += 40;
            }
        }

        if (gameState.PlayerCurrentEnergy <= 1 && card.EnergyCost?.GetResolved() == 0)
        {
            score += 60;
        }

        var enemies = combatState.HittableEnemies;
        if (enemies.Count > 0)
        {
            bool hasLowHpEnemy = false;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].CurrentHp <= 15)
                {
                    hasLowHpEnemy = true;
                    break;
                }
            }

            if (hasLowHpEnemy && (card.DynamicVars?.TryGetValue("Damage", out var _) ?? false))
            {
                score += 70;
            }
        }

        if (gameState.HandSize <= 2 && (card.DynamicVars?.TryGetValue("Cards", out var _) ?? false))
        {
            score += 50;
        }

        return score;
    }

    private int GetCardBlockAmount(CardModel card)
    {
        if (card.DynamicVars?.TryGetValue("Block", out var blockVar) ?? false)
        {
            return blockVar != null ? (int)blockVar.BaseValue : 0;
        }
        return 0;
    }

    private int EvaluateComboPotential(CardModel card, Player player, CombatState combatState)
    {
        int score = 0;

        if (card.Type == CardType.Skill)
        {
            var hand = PileType.Hand.GetPile(player);
            for (int i = 0; i < hand.Cards.Count; i++)
            {
                var otherCard = hand.Cards[i];
                if (otherCard == card) continue;
                if (GetSpecialCardType(otherCard) == SpecialCardType.FollowThrough)
                {
                    score += 40;
                    break;
                }
            }
        }

        if (card.DynamicVars?.ContainsKey("VulnerablePower") ?? false)
        {
            var hand = PileType.Hand.GetPile(player);
            for (int i = 0; i < hand.Cards.Count; i++)
            {
                var otherCard = hand.Cards[i];
                if (otherCard == card) continue;
                if (GetSpecialCardType(otherCard) == SpecialCardType.Dismantle)
                {
                    score += 50;
                    break;
                }
            }
        }

        if (card.DynamicVars?.ContainsKey("PoisonPower") ?? false)
        {
            var hand = PileType.Hand.GetPile(player);
            for (int i = 0; i < hand.Cards.Count; i++)
            {
                var otherCard = hand.Cards[i];
                if (otherCard == card) continue;
                if (GetSpecialCardType(otherCard) == SpecialCardType.BubbleBubble)
                {
                    score += 45;
                    break;
                }
            }
        }

        return score;
    }

    private int EvaluateZeroCostCards(CardModel card, Player player, GameStateInfo gameState)
    {
        int score = 0;
        var energyCost = card.EnergyCost;
        if (energyCost == null) return 0;

        int cost = energyCost.GetResolved();
        
        if (cost == 0)
        {
            score += ScoreWeights.ZeroCostBonus;

            var dynamicVars = card.DynamicVars;
            if (dynamicVars != null)
            {
                if (dynamicVars.TryGetValue("Energy", out var energyVar) && energyVar != null)
                {
                    score += ScoreWeights.CardDrawBase + 20;
                }

                if (dynamicVars.TryGetValue("Cards", out var cardsVar) && cardsVar != null)
                {
                    score += ScoreWeights.CardDrawBase;
                }

                if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
                {
                    score += ScoreWeights.BlockBase - 10;
                }

                if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
                {
                    score += ScoreWeights.DamageBase - 10;
                }

                bool hasAnyPositiveEffect = energyVar != null ||
                                          cardsVar != null ||
                                          blockVar != null ||
                                          damageVar != null ||
                                          dynamicVars.Any(kvp => kvp.Key.EndsWith("Power"));
                
                if (!hasAnyPositiveEffect && card.Type == CardType.Skill)
                {
                    score += ScoreWeights.ComboBonus;
                }
            }

            if (gameState.PlayerCurrentEnergy <= 2)
            {
                score += ScoreWeights.EnergyEfficiencyBonus;
            }

            if (gameState.HandSize <= 4)
            {
                score += 30;
            }
        }

        return score;
    }

    private int EvaluateLowHpEnemyFinisher(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int score = 0;
        var enemies = combatState.HittableEnemies;
        if (enemies.Count == 0) return 0;

        bool hasLowHpEnemy = false;
        int lowestEnemyHp = int.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            int hp = (int)enemy.CurrentHp;
            if (hp < lowestEnemyHp)
            {
                lowestEnemyHp = hp;
                if (hp <= 20)
                {
                    hasLowHpEnemy = true;
                }
            }
        }

        if (!hasLowHpEnemy) return 0;

        var dynamicVars = card.DynamicVars;
        
        if (dynamicVars != null && dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
        {
            var damage = (int)damageVar.BaseValue;
            
            if (damage >= lowestEnemyHp)
            {
                score += ScoreWeights.FinisherBonus;
                MainFile.Logger.Debug($"Vakuu: Finisher potential - {card.Title} deals {damage} to enemy with {lowestEnemyHp} HP");
            }
            else if (damage >= (int)(lowestEnemyHp * 0.8))
            {
                score += ScoreWeights.ZeroCostBonus;
            }
            else if (damage >= (int)(lowestEnemyHp * 0.5))
            {
                score += ScoreWeights.BlockBase + 10;
            }

            if (gameState.EnemiesWithVulnerable > 0)
            {
                score += ScoreWeights.EnergyEfficiencyBonus;
            }
        }

        if (card.Tags.Contains(CardTag.Strike))
        {
            score += ScoreWeights.ComboBonus;
        }

        if (lowestEnemyHp <= 10)
        {
            score += ScoreWeights.CardDrawBase + 20;
        }

        return score;
    }

    private int EvaluateDeckManipulationCards(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int score = 0;
        var cardName = card.GetType().Name;
        var dynamicVars = card.DynamicVars;

        if (cardName == "StealCard")
        {
            var targetPlayer = GetMultiplayerTarget(player);
            if (targetPlayer != null)
            {
                var drawPile = PileType.Draw.GetPile(targetPlayer);
                int drawPileSize = drawPile.Cards.Count;
                
                if (drawPileSize >= 5)
                {
                    score += 180;
                    MainFile.Logger.Debug($"Vakuu: StealCard has {drawPileSize} cards in target's draw pile");
                }
                else if (drawPileSize >= 3)
                {
                    score += 120;
                }
                else
                {
                    score += 60;
                }

                var hand = PileType.Hand.GetPile(player);
                if (hand.Cards.Count <= 3)
                {
                    score += 40;
                }
            }
            else
            {
                score -= 200;
            }
        }

        if (cardName == "GiveYou")
        {
            var targetPlayer = GetMultiplayerTarget(player);
            if (targetPlayer != null)
            {
                var hand = PileType.Hand.GetPile(player);
                var usefulCards = hand.Cards
                    .Where(c => c != card && c.CanPlay())
                    .ToList();

                if (usefulCards.Count >= 2)
                {
                    score += 160;
                    MainFile.Logger.Debug($"Vakuu: GiveYou can transfer one of {usefulCards.Count} useful cards");
                }
                else if (usefulCards.Count == 1)
                {
                    score += 100;
                }
                else
                {
                    score += 40;
                }
            }
            else
            {
                score -= 200;
            }
        }

        if (dynamicVars != null)
        {
            if (dynamicVars.ContainsKey("Cards") && dynamicVars["Cards"].IntValue > 0)
            {
                int cardsToDraw = dynamicVars["Cards"].IntValue;
                var drawPile = PileType.Draw.GetPile(player);
                int drawPileSize = drawPile.Cards.Count;

                if (drawPileSize > 0)
                {
                    score += 70 + (cardsToDraw * 20);
                    
                    if (gameState.HandSize <= 3)
                    {
                        score += 40;
                    }
                }
            }

            if (dynamicVars.ContainsKey("Exhaust"))
            {
                if (gameState.HandSize <= 2)
                {
                    score -= 30;
                }
            }
        }

        return score;
    }

    private int EvaluateMultiplayerCards(CardModel card, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int score = 0;
        var cardName = card.GetType().Name;

        if (cardName == "PigSacrifice")
        {
            var targetPlayer = GetMultiplayerTarget(player);
            if (targetPlayer != null)
            {
                var ownerCreature = player.Creature;
                int hpToTransfer = IsUpgradedCard(card) ? ownerCreature.CurrentHp : ownerCreature.CurrentHp / 2;
                int blockToTransfer = IsUpgradedCard(card) ? ownerCreature.Block : ownerCreature.Block / 2;

                var targetCreature = targetPlayer.Creature;
                int targetHpPercent = (int)(targetCreature.CurrentHp * 100 / targetCreature.MaxHp);
                int ownerHpPercent = (int)(ownerCreature.CurrentHp * 100 / ownerCreature.MaxHp);

                if (targetHpPercent < 50 && ownerHpPercent > 60)
                {
                    score += 200;
                    MainFile.Logger.Debug($"Vakuu: PigSacrifice can save low HP ally ({targetHpPercent}%) using owner HP ({ownerHpPercent}%)");
                }
                else if (targetHpPercent < 70 && ownerHpPercent > 70)
                {
                    score += 140;
                }

                if (blockToTransfer > 0)
                {
                    score += 50;
                }

                if (IsUpgradedCard(card))
                {
                    score += 60;
                }
            }
            else
            {
                score -= 200;
            }
        }

        return score;
    }

    private Player? GetMultiplayerTarget(Player sourcePlayer)
    {
        var runState = sourcePlayer.RunState;
        if (runState == null) return null;

        var otherPlayers = runState.Players?.Where(p => p != sourcePlayer).ToList();
        return otherPlayers?.FirstOrDefault();
    }

    private bool IsUpgradedCard(CardModel card)
    {
        return card.CurrentUpgradeLevel > 0;
    }

    private void UseOptimalPotion(PlayerChoiceContext choiceContext, Player player, CombatState combatState, GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        var potionSlots = player.PotionSlots;
        if (potionSlots == null || potionSlots.Count == 0)
        {
            MainFile.Logger.Debug("Vakuu: No potion slots available");
            return;
        }

        var potions = potionSlots.Where(p => p != null).ToList();
        if (potions.Count == 0)
        {
            MainFile.Logger.Debug("Vakuu: No potions available");
            return;
        }

        MainFile.Logger.Info($"Vakuu: Evaluating {potions.Count} potions");

        PotionModel? bestPotion = null;
        int bestScore = int.MinValue;
        Creature? bestTarget = null;

        for (int i = 0; i < potions.Count; i++)
        {
            var potion = potions[i];
            if (potion == null) continue;

            var target = GetBestTargetForPotion(potion, player, combatState, gameState, enemyCache);
            int score = EvaluatePotion(potion, player, combatState, gameState, target, enemyCache);

            MainFile.Logger.Debug($"Vakuu: Potion {potion.Title} score: {score}");

            if (score > bestScore)
            {
                bestScore = score;
                bestPotion = potion;
                bestTarget = target;
            }
        }

        if (bestPotion != null && bestScore > 100)
        {
            MainFile.Logger.Info($"Vakuu: Using potion {bestPotion.Title} with score {bestScore}");
            try
            {
                bestPotion.EnqueueManualUse(bestTarget);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Vakuu: Failed to use potion: {ex.Message}");
            }
        }
        else
        {
            MainFile.Logger.Debug("Vakuu: No suitable potion to use this turn");
        }
    }

    private int EvaluatePotion(PotionModel potion, Player player, CombatState combatState, GameStateInfo gameState, Creature? target, EnemyCache[] enemyCache)
    {
        int score = 100;

        var potionName = potion.GetType().Name;
        var dynamicVars = potion.DynamicVars;

        if (potionName.Contains("Health") || potionName.Contains("Heal") || potionName.Contains("PotionOfHealing"))
        {
            int healAmount = GetPotionHealAmount(potion, player);
            int missingHp = gameState.PlayerMaxHp - gameState.PlayerCurrentHp;
            
            if (gameState.PlayerHpPercent < 30)
            {
                score += 200;
                
                if (gameState.IsInDanger && healAmount >= missingHp)
                {
                    score += 100;
                    MainFile.Logger.Debug($"Vakuu: Health potion can save player (heal:{healAmount}, missing:{missingHp})");
                }
            }
            else if (gameState.PlayerHpPercent < 50)
            {
                score += 150;
            }
            else if (gameState.PlayerHpPercent < 70)
            {
                score += 80;
            }
            else
            {
                score -= 50;
            }
            
            if (healAmount > missingHp)
            {
                score -= 30;
                MainFile.Logger.Debug($"Vakuu: Health potion overheal (heal:{healAmount}, missing:{missingHp})");
            }
        }

        if (potionName.Contains("Strength") || potionName.Contains("Attack"))
        {
            if (!gameState.EnemyIntendsAttack)
            {
                score += 80;
            }
            if (gameState.EnemiesWithVulnerable > 0)
            {
                score += 60;
            }
            if (dynamicVars != null && dynamicVars.ContainsKey("Strength"))
            {
                var strengthVar = dynamicVars.Strength;
                if (strengthVar != null && strengthVar.BaseValue >= 3)
                {
                    score += 50;
                }
            }
            
            int remainingAttackCards = CountRemainingAttackCardsInHand(player, null, 0);
            if (remainingAttackCards > 0)
            {
                score += remainingAttackCards * 20;
                MainFile.Logger.Debug($"Vakuu: Strength potion will boost {remainingAttackCards} attack cards");
            }
        }

        if (potionName.Contains("Block") || potionName.Contains("Defense"))
        {
            if (gameState.NeedsBlock)
            {
                score += 150;
            }
            if (gameState.IsInDanger)
            {
                score += 120;
            }
            
            int blockAmount = GetPotionBlockAmount(potion);
            int effectiveBlockNeeded = CalculateEffectiveBlockNeeded(gameState, blockAmount);
            if (effectiveBlockNeeded < blockAmount)
            {
                float overflowRatio = (blockAmount - effectiveBlockNeeded) / (float)blockAmount;
                if (overflowRatio > 0.5f)
                {
                    score -= 40;
                    MainFile.Logger.Debug($"Vakuu: Block potion has {overflowRatio:P0} overflow");
                }
            }
        }

        if (potionName.Contains("Energy") || potionName.Contains("Mana"))
        {
            if (gameState.PlayerCurrentEnergy <= 1)
            {
                score += 180;
            }
            else if (gameState.PlayerCurrentEnergy <= 2)
            {
                score += 100;
            }
            else
            {
                score += 50;
            }
            
            int handValue = CalculateHandValueWithoutCard(null, player, combatState, gameState);
            if (handValue > 200)
            {
                score += 60;
                MainFile.Logger.Debug($"Vakuu: Energy potion enables high value hand (value:{handValue})");
            }
        }

        if (potionName.Contains("Weak") || potionName.Contains("Vulnerable"))
        {
            if (target != null)
            {
                if (!EnemyHasDebuff(target, potionName.Contains("Weak") ? "WeakPower" : "VulnerablePower"))
                {
                    score += 100;
                }
                if (target.Monster?.IntendsToAttack == true)
                {
                    score += 80;
                }
                
                string debuffType = potionName.Contains("Weak") ? "WeakPower" : "VulnerablePower";
                int enemiesWithoutDebuff = CountEnemiesWithoutDebuff(enemyCache, debuffType);
                if (enemiesWithoutDebuff > 1)
                {
                    score += (enemiesWithoutDebuff - 1) * 30;
                    MainFile.Logger.Debug($"Vakuu: Debuff potion can affect {enemiesWithoutDebuff} enemies");
                }
            }
        }

        if (potionName.Contains("Fire") || potionName.Contains("Damage") || potionName.Contains("Attack"))
        {
            var enemies = combatState.HittableEnemies;
            if (enemies.Count > 0)
            {
                bool hasLowHpEnemy = CheckLowHpEnemyExistsFast(enemyCache, 20);
                
                if (hasLowHpEnemy)
                {
                    score += 120;
                }
                else
                {
                    score += 60;
                }
                
                int potionDamage = GetPotionDamageAmount(potion);
                int totalEnemyHp = GetTotalEnemyRemainingHp(enemyCache);
                if (potionDamage >= totalEnemyHp * 0.5)
                {
                    score += 80;
                    MainFile.Logger.Debug($"Vakuu: Damage potion can deal significant damage ({potionDamage} vs {totalEnemyHp})");
                }
            }
        }

        if (potionName.Contains("Regen") || potionName.Contains("Regeneration"))
        {
            if (gameState.PlayerHpPercent < 60)
            {
                score += 130;
            }
            else
            {
                score += 70;
            }
            
            if (!gameState.IsInDanger)
            {
                score += 40;
                MainFile.Logger.Debug("Vakuu: Regen potion good for sustained combat");
            }
        }

        if (potionName.Contains("Cultist") || potionName.Contains("Summon"))
        {
            score += 90;
            
            if (gameState.EnemyCount > 1)
            {
                score += 40;
                MainFile.Logger.Debug("Vakuu: Summon potion effective against multiple enemies");
            }
        }

        if (potionName.Contains("Focus"))
        {
            score += 110;
            
            if (CountCardsWithPowerEffectsInHand(player) > 0)
            {
                score += 50;
                MainFile.Logger.Debug("Vakuu: Focus potion boosts power cards in hand");
            }
        }

        if (potionName.Contains("Speed") || potionName.Contains("Quick"))
        {
            if (gameState.NeedsBlock)
            {
                score += 70;
            }
            else
            {
                score += 50;
            }
        }

        if (potionName.Contains("Intangible") || potionName.Contains("Invincible"))
        {
            if (gameState.IsInDanger || gameState.PlayerHpPercent < 40)
            {
                score += 200;
                MainFile.Logger.Debug("Vakuu: Intangible potion crucial for survival");
            }
            else if (gameState.IncomingDamage > gameState.PlayerBlock + 30)
            {
                score += 120;
            }
            else
            {
                score += 60;
            }
        }

        if (potionName.Contains("Buffer") || potionName.Contains("Shield"))
        {
            if (gameState.IsInDanger)
            {
                score += 150;
            }
            else if (gameState.PlayerHpPercent < 60)
            {
                score += 100;
            }
            else
            {
                score += 60;
            }
        }

        if (potionName.Contains("Poison"))
        {
            int enemiesWithoutPoison = CountEnemiesWithoutDebuff(enemyCache, "PoisonPower");
            if (enemiesWithoutPoison > 0)
            {
                score += enemiesWithoutPoison * 50;
                
                var hand = PileType.Hand.GetPile(player);
                for (int i = 0; i < hand.Cards.Count; i++)
                {
                    if (GetSpecialCardType(hand.Cards[i]) == SpecialCardType.BubbleBubble)
                    {
                        score += 60;
                        MainFile.Logger.Debug("Vakuu: Poison potion + BubbleBubble combo");
                        break;
                    }
                }
            }
            else
            {
                score -= 40;
            }
        }

        if (gameState.IsInDanger && score < 150)
        {
            score += 50;
        }
        
        if (gameState.CurrentBloodState == BloodHealthState.Low)
        {
            if (potionName.Contains("Health") || potionName.Contains("Heal") || 
                potionName.Contains("Block") || potionName.Contains("Defense") ||
                potionName.Contains("Intangible") || potionName.Contains("Buffer"))
            {
                score = (int)(score * 1.3f);
                MainFile.Logger.Debug("Vakuu: Survival potion prioritized in low HP state");
            }
            else if (potionName.Contains("Strength") || potionName.Contains("Attack"))
            {
                int totalEnemyHp = GetTotalEnemyRemainingHp(enemyCache);
                int nonHealthDamage = CalculateNonHealthCostDamage(
                    PileType.Hand.GetPile(player).Cards, 
                    player, 
                    combatState, 
                    gameState
                );
                
                if (nonHealthDamage + GetPotionDamageAmount(potion) >= totalEnemyHp)
                {
                    score = (int)(score * 1.4f);
                    MainFile.Logger.Debug("Vakuu: Offensive potion can finish enemy despite low HP");
                }
                else
                {
                    score -= 30;
                }
            }
        }

        return score;
    }

    private Creature? GetBestTargetForPotion(PotionModel potion, Player player, CombatState combatState, GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        var potionName = potion.GetType().Name;

        if (potion.TargetType == TargetType.Self || potionName.Contains("Health") || potionName.Contains("Heal") || potionName.Contains("Energy"))
        {
            return player.Creature;
        }

        if (potion.TargetType == TargetType.AnyEnemy || potion.TargetType == TargetType.AllEnemies)
        {
            if (potionName.Contains("Weak") || potionName.Contains("Vulnerable"))
            {
                Creature? bestTarget = SelectHighestThreatEnemyWithoutDebuff(enemyCache, potionName.Contains("Weak") ? "WeakPower" : "VulnerablePower");
                if (bestTarget != null) return bestTarget;
            }
            
            if (potionName.Contains("Poison"))
            {
                Creature? bestTarget = SelectHighestMaxHpEnemyWithoutPoison(enemyCache);
                if (bestTarget != null) return bestTarget;
            }

            if (enemyCache.Length > 0)
            {
                return SelectLowestHpEnemyOptimized(enemyCache);
            }
        }

        if (potion.TargetType == TargetType.AnyAlly || potion.TargetType == TargetType.AllAllies)
        {
            return SelectBestAllyTarget(combatState, player.Creature);
        }

        return player.Creature;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Creature? SelectHighestThreatEnemyWithoutDebuff(EnemyCache[] enemyCache, string debuffType)
    {
        Creature? bestTarget = null;
        int highestThreat = -1;

        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.Enemy == null || !ec.Enemy.IsAlive) continue;

            bool hasDebuff = debuffType switch
            {
                "WeakPower" => ec.HasWeak,
                "VulnerablePower" => ec.HasVulnerable,
                "PoisonPower" => ec.HasPoison,
                _ => false
            };

            if (!hasDebuff && ec.ThreatScore > highestThreat)
            {
                highestThreat = ec.ThreatScore;
                bestTarget = ec.Enemy;
            }
        }

        return bestTarget;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Creature? SelectHighestMaxHpEnemyWithoutPoison(EnemyCache[] enemyCache)
    {
        Creature? bestTarget = null;
        int highestMaxHp = 0;

        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.Enemy == null || !ec.Enemy.IsAlive || ec.HasPoison) continue;

            if (ec.MaxHp > highestMaxHp)
            {
                highestMaxHp = ec.MaxHp;
                bestTarget = ec.Enemy;
            }
        }

        return bestTarget;
    }

    private int CalculateHandValue(IReadOnlyList<CardModel> hand, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int totalValue = 0;
        
        for (int i = 0; i < hand.Count; i++)
        {
            var card = hand[i];
            if (!card.CanPlay()) continue;
            
            var dynamicVars = card.DynamicVars;
            if (dynamicVars == null) continue;
            
            if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
            {
                totalValue += (int)damageVar.BaseValue * 10;
            }
            
            if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
            {
                totalValue += (int)blockVar.BaseValue * 12;
            }
            
            foreach (var kvp in dynamicVars)
            {
                if (kvp.Key.EndsWith("Power") && BuffPowerNames.Contains(kvp.Key))
                {
                    if (kvp.Value is DynamicVar powerVar)
                    {
                        int baseScore = BuffPriorityScores.TryGetValue(kvp.Key, out var score) ? score : 50;
                        totalValue += baseScore + (int)powerVar.BaseValue * 5;
                    }
                }
            }
        }
        
        return totalValue;
    }

    private int CalculateTotalBlockFromHand(IReadOnlyList<CardModel> hand, Player player, CombatState combatState)
    {
        int totalBlock = 0;
        
        for (int i = 0; i < hand.Count; i++)
        {
            var card = hand[i];
            if (!card.CanPlay()) continue;
            
            var dynamicVars = card.DynamicVars;
            if (dynamicVars == null) continue;
            
            if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
            {
                totalBlock += (int)blockVar.BaseValue;
            }
        }
        
        return totalBlock;
    }

    private Dictionary<string, int> AnalyzeBuffEffectsFromHand(IReadOnlyList<CardModel> hand, Player player, CombatState combatState)
    {
        var buffEffects = new Dictionary<string, int>();
        
        for (int i = 0; i < hand.Count; i++)
        {
            var card = hand[i];
            if (!card.CanPlay()) continue;
            
            var dynamicVars = card.DynamicVars;
            if (dynamicVars == null) continue;
            
            foreach (var kvp in dynamicVars)
            {
                if (kvp.Key.EndsWith("Power") && BuffPowerNames.Contains(kvp.Key))
                {
                    if (kvp.Value is DynamicVar powerVar)
                    {
                        if (!buffEffects.ContainsKey(kvp.Key))
                        {
                            buffEffects[kvp.Key] = 0;
                        }
                        buffEffects[kvp.Key] += (int)powerVar.BaseValue;
                    }
                }
            }
        }
        
        return buffEffects;
    }

    private bool ShouldUseOneCostDrawCard(CardModel card, Player player, CombatState combatState, GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        var energyCost = card.EnergyCost?.GetResolved() ?? 999;
        if (energyCost != 1) return true;
        
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return true;
        
        if (!dynamicVars.TryGetValue("Cards", out var cardsVar) || cardsVar == null) return true;
        
        if (cardsVar.IntValue < 1) return true;
        
        var hand = PileType.Hand.GetPile(player);
        var otherCards = hand.Cards.Where(c => c != card && c.CanPlay()).ToList();
        
        int currentHandValue = CalculateHandValue(otherCards, player, combatState, gameState);
        int currentBlock = CalculateTotalBlockFromHand(otherCards, player, combatState);
        var currentBuffs = AnalyzeBuffEffectsFromHand(otherCards, player, combatState);
        
        int neededBlock = gameState.IncomingDamage - player.Creature.Block;
        bool needsEmergencyBlock = neededBlock > 0 && neededBlock > currentBlock;
        
        bool hasLowHpEnemy = CheckLowHpEnemyExistsFast(enemyCache, 15);
        int totalEnemyHp = GetTotalEnemyRemainingHp(enemyCache);
        int nonHealthDamage = CalculateNonHealthCostDamage(otherCards, player, combatState, gameState);
        bool canFinishWithoutHealthCost = nonHealthDamage >= totalEnemyHp;
        
        if (needsEmergencyBlock && !card.Tags.Contains(CardTag.Defend))
        {
            MainFile.Logger.Debug($"Vakuu: Skip 1-cost draw card - need emergency block");
            return false;
        }
        
        if (hasLowHpEnemy && !canFinishWithoutHealthCost)
        {
            MainFile.Logger.Debug($"Vakuu: Skip 1-cost draw card - should finish enemy first");
            return false;
        }
        
        if (currentHandValue > 300 && gameState.HandSize >= 5)
        {
            MainFile.Logger.Debug($"Vakuu: Skip 1-cost draw card - hand value already high ({currentHandValue})");
            return false;
        }
        
        int energyAfterPlay = (player.PlayerCombatState?.Energy ?? 0) - 1;
        if (energyAfterPlay == 0 && currentHandValue > 150)
        {
            MainFile.Logger.Debug($"Vakuu: Skip 1-cost draw card - no energy to use drawn cards");
            return false;
        }
        
        MainFile.Logger.Debug($"Vakuu: Allow 1-cost draw card - handValue:{currentHandValue}, energy:{energyAfterPlay}");
        return true;
    }

    private int GetTotalEnemyRemainingHp(EnemyCache[] enemyCache)
    {
        int totalHp = 0;
        
        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.Enemy != null && ec.Enemy.IsAlive)
            {
                totalHp += ec.CurrentHp;
            }
        }
        
        return totalHp;
    }

    private int CalculateNonHealthCostDamage(IReadOnlyList<CardModel> hand, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int totalDamage = 0;
        
        for (int i = 0; i < hand.Count; i++)
        {
            var card = hand[i];
            if (!card.CanPlay()) continue;
            
            if (IsHealthCostCard(card)) continue;
            
            var dynamicVars = card.DynamicVars;
            if (dynamicVars == null) continue;
            
            if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
            {
                int damage = (int)damageVar.BaseValue;
                
                var enemies = combatState.HittableEnemies;
                for (int j = 0; j < enemies.Count; j++)
                {
                    var enemy = enemies[j];
                    if (enemy.HasPower<VulnerablePower>())
                    {
                        damage = (int)(damage * 1.5);
                    }
                    if (enemy.HasPower<WeakPower>())
                    {
                        damage = (int)(damage * 0.75);
                    }
                }
                
                totalDamage += damage;
            }
        }
        
        int strength = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        totalDamage += strength * hand.Count(c => c.Type == CardType.Attack && !IsHealthCostCard(c));
        
        return totalDamage;
    }

    private bool IsHealthCostCard(CardModel card)
    {
        var cardName = card.GetType().Name;
        
        if (cardName.Contains("Health") || cardName.Contains("Blood") || cardName.Contains("Sacrifice"))
        {
            return true;
        }
        
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return false;
        
        if (dynamicVars.ContainsKey("HealthCost") || dynamicVars.ContainsKey("HPCost"))
        {
            return true;
        }
        
        return false;
    }

    private bool ShouldUseHealthCostCard(CardModel card, Player player, CombatState combatState, GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        if (!IsHealthCostCard(card)) return true;
        
        int playerCurrentHp = (int)player.Creature.CurrentHp;
        int healthCost = GetHealthCost(card, player);
        
        if (playerCurrentHp - healthCost <= 0)
        {
            var incomingDamage = gameState.IncomingDamage;
            int block = (int)player.Creature.Block;
            
            if (incomingDamage > block && playerCurrentHp - healthCost <= incomingDamage - block)
            {
                int totalEnemyHp = GetTotalEnemyRemainingHp(enemyCache);
                int nonHealthDamage = CalculateNonHealthCostDamage(
                    PileType.Hand.GetPile(player).Cards, 
                    player, 
                    combatState, 
                    gameState
                );
                
                if (nonHealthDamage >= totalEnemyHp)
                {
                    MainFile.Logger.Debug($"Vakuu: Allow health cost card - can finish enemy despite HP risk");
                    return true;
                }
                
                MainFile.Logger.Debug($"Vakuu: Skip health cost card - would die ({playerCurrentHp} - {healthCost})");
                return false;
            }
        }
        
        int totalEnemyHp2 = GetTotalEnemyRemainingHp(enemyCache);
        var hand = PileType.Hand.GetPile(player).Cards;
        var otherCards = hand.Where(c => c != card && c.CanPlay()).ToList();
        int nonHealthDamage2 = CalculateNonHealthCostDamage(otherCards, player, combatState, gameState);
        
        if (nonHealthDamage2 >= totalEnemyHp2)
        {
            MainFile.Logger.Debug($"Vakuu: Skip health cost card - can finish without it (dmg:{nonHealthDamage2} vs hp:{totalEnemyHp2})");
            return false;
        }
        
        if (playerCurrentHp - healthCost <= 5 && gameState.EnemyIntendsAttack)
        {
            MainFile.Logger.Debug($"Vakuu: Skip health cost card - HP too low ({playerCurrentHp} - {healthCost})");
            return false;
        }
        
        var dynamicVars = card.DynamicVars;
        if (dynamicVars != null && dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
        {
            int damage = (int)damageVar.BaseValue;
            
            for (int i = 0; i < enemyCache.Length; i++)
            {
                var ec = enemyCache[i];
                if (ec.CurrentHp <= damage && ec.CurrentHp > 0)
                {
                    MainFile.Logger.Debug($"Vakuu: Allow health cost card - can finish enemy ({damage} >= {ec.CurrentHp})");
                    return true;
                }
            }
        }
        
        if (gameState.IsInDanger && playerCurrentHp - healthCost > 10)
        {
            MainFile.Logger.Debug($"Vakuu: Allow health cost card - emergency situation");
            return true;
        }
        
        MainFile.Logger.Debug($"Vakuu: Allow health cost card - strategic use");
        return true;
    }

    private int GetHealthCost(CardModel card, Player player)
    {
        var dynamicVars = card.DynamicVars;
        if (dynamicVars == null) return 0;
        
        if (dynamicVars.ContainsKey("HealthCost"))
        {
            var healthCostVar = dynamicVars["HealthCost"];
            if (healthCostVar != null)
            {
                return (int)healthCostVar.BaseValue;
            }
        }
        
        if (dynamicVars.ContainsKey("HPCost"))
        {
            var hpCostVar = dynamicVars["HPCost"];
            if (hpCostVar != null)
            {
                return (int)hpCostVar.BaseValue;
            }
        }
        
        var cardName = card.GetType().Name;
        if (cardName == "PigSacrifice")
        {
            return IsUpgradedCard(card) ? player.Creature.CurrentHp : player.Creature.CurrentHp / 2;
        }
        
        return 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetPotionHealAmount(PotionModel potion, Player player)
    {
        var dynamicVars = potion.DynamicVars;
        if (dynamicVars == null) return 0;
        
        if (dynamicVars.TryGetValue("Heal", out var healVar) && healVar != null)
        {
            return (int)healVar.BaseValue;
        }
        
        if (dynamicVars.TryGetValue("Health", out var healthVar) && healthVar != null)
        {
            return (int)healthVar.BaseValue;
        }
        
        return player.Creature.MaxHp / 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetPotionBlockAmount(PotionModel potion)
    {
        var dynamicVars = potion.DynamicVars;
        if (dynamicVars == null) return 0;
        
        if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
        {
            return (int)blockVar.BaseValue;
        }
        
        return 15;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetPotionDamageAmount(PotionModel potion)
    {
        var dynamicVars = potion.DynamicVars;
        if (dynamicVars == null) return 0;
        
        if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
        {
            return (int)damageVar.BaseValue;
        }
        
        return 10;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountEnemiesWithoutDebuff(EnemyCache[] enemyCache, string debuffType)
    {
        int count = 0;
        
        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.Enemy == null || !ec.Enemy.IsAlive) continue;
            
            bool hasDebuff = debuffType switch
            {
                "WeakPower" => ec.HasWeak,
                "VulnerablePower" => ec.HasVulnerable,
                "PoisonPower" => ec.HasPoison,
                _ => false
            };
            
            if (!hasDebuff)
            {
                count++;
            }
        }
        
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CalculateHandValueWithoutCard(CardModel? excludeCard, Player player, CombatState combatState, GameStateInfo gameState)
    {
        int totalValue = 0;
        var hand = PileType.Hand.GetPile(player);
        if (hand == null) return 0;
        
        for (int i = 0; i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            if (card == excludeCard || !card.CanPlay()) continue;
            
            var dynamicVars = card.DynamicVars;
            if (dynamicVars == null) continue;
            
            if (dynamicVars.TryGetValue("Damage", out var damageVar) && damageVar != null)
            {
                totalValue += (int)damageVar.BaseValue * 10;
            }
            
            if (dynamicVars.TryGetValue("Block", out var blockVar) && blockVar != null)
            {
                totalValue += (int)blockVar.BaseValue * 12;
            }
            
            foreach (var kvp in dynamicVars)
            {
                if (kvp.Key.EndsWith("Power") && BuffPowerNames.Contains(kvp.Key))
                {
                    if (kvp.Value is DynamicVar powerVar)
                    {
                        int baseScore = BuffPriorityScores.TryGetValue(kvp.Key, out var score) ? score : 50;
                        totalValue += baseScore + (int)powerVar.BaseValue * 5;
                    }
                }
            }
        }
        
        return totalValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountCardsWithPowerEffectsInHand(Player player)
    {
        int count = 0;
        var hand = PileType.Hand.GetPile(player);
        if (hand == null) return 0;
        
        for (int i = 0; i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            if (!card.CanPlay()) continue;
            
            var dynamicVars = card.DynamicVars;
            if (dynamicVars == null) continue;
            
            foreach (var kvp in dynamicVars)
            {
                if (kvp.Key.EndsWith("Power") && BuffPowerNames.Contains(kvp.Key))
                {
                    count++;
                    break;
                }
            }
        }
        
        return count;
    }

    private CardPriorityLevel GetCardPriorityLevel(CardModel card)
    {
        if (card.Tags.Contains(CardTag.Defend))
        {
            return CardPriorityLevel.Survival;
        }
        
        var dynamicVars = card.DynamicVars;
        if (dynamicVars != null)
        {
            if (dynamicVars.ContainsKey("Heal") || dynamicVars.ContainsKey("Block") || 
                dynamicVars.ContainsKey("Intangible") || dynamicVars.ContainsKey("Buffer") ||
                dynamicVars.ContainsKey("Artifact"))
            {
                return CardPriorityLevel.Survival;
            }
            
            foreach (var kvp in dynamicVars)
            {
                if (kvp.Key.EndsWith("Power") && PowerToPriority.TryGetValue(kvp.Key, out var priority))
                {
                    if (priority == CardPriorityLevel.Survival || priority == CardPriorityLevel.Control)
                    {
                        return priority;
                    }
                }
            }
            
            if (dynamicVars.ContainsKey("Weak") || dynamicVars.ContainsKey("Vulnerable") ||
                dynamicVars.ContainsKey("Frail"))
            {
                return CardPriorityLevel.Control;
            }
            
            if (dynamicVars.ContainsKey("Damage") || dynamicVars.ContainsKey("Strike"))
            {
                return CardPriorityLevel.Offensive;
            }
        }
        
        return CardPriorityLevel.Utility;
    }

    private int EvaluateCardWithBloodState(CardModel card, Player player, CombatState combatState, 
        GameStateInfo gameState, EnemyCache[] enemyCache)
    {
        int baseScore = EvaluateCardOptimized(card, player, combatState, gameState, enemyCache);
        
        CardPriorityLevel cardPriority = GetCardPriorityLevel(card);
        BloodHealthState bloodState = gameState.CurrentBloodState;
        float smoothFactor = gameState.BloodStateSmoothFactor;
        
        float survivalMultiplier = 1.0f;
        float controlMultiplier = 1.0f;
        float offensiveMultiplier = 1.0f;
        
        switch (bloodState)
        {
            case BloodHealthState.Low:
                survivalMultiplier = Multipliers.LowHpSurvival - (smoothFactor * 0.5f);
                controlMultiplier = Multipliers.LowHpControl - (smoothFactor * 0.5f);
                offensiveMultiplier = Multipliers.LowHpOffensive + (smoothFactor * 0.2f);
                break;
            case BloodHealthState.Medium:
                survivalMultiplier = Multipliers.MediumHpSurvival + (smoothFactor * 0.5f);
                controlMultiplier = Multipliers.MediumHpControl + (smoothFactor * 0.3f);
                offensiveMultiplier = Multipliers.MediumHpOffensive + (smoothFactor * 0.3f);
                break;
            case BloodHealthState.High:
                survivalMultiplier = Multipliers.HighHpSurvival + (smoothFactor * 0.2f);
                controlMultiplier = Multipliers.HighHpControl + (smoothFactor * 0.1f);
                offensiveMultiplier = Multipliers.HighHpOffensive + (smoothFactor * 0.5f);
                break;
        }
        
        int threatLevel = CalculateEnemyThreatLevel(enemyCache, gameState);
        float threatMultiplier = 1.0f + (threatLevel / 100.0f);
        
        if (bloodState == BloodHealthState.Low && gameState.EnemyIntendsAttack)
        {
            threatMultiplier *= 1.5f;
        }
        
        int adjustedScore = cardPriority switch
        {
            CardPriorityLevel.Survival => (int)(baseScore * survivalMultiplier * threatMultiplier),
            CardPriorityLevel.Control => (int)(baseScore * controlMultiplier),
            CardPriorityLevel.Offensive => (int)(baseScore * offensiveMultiplier),
            CardPriorityLevel.Utility => baseScore,
            _ => baseScore
        };
        
        if (bloodState == BloodHealthState.Low && cardPriority == CardPriorityLevel.Offensive)
        {
            var damageVar = card.DynamicVars?.Damage;
            if (damageVar != null)
            {
                int damage = (int)damageVar.BaseValue;
                int totalEnemyHp = GetTotalEnemyRemainingHp(enemyCache);
                
                if (damage >= totalEnemyHp)
                {
                    adjustedScore = (int)(adjustedScore * 1.8f);
                    MainFile.Logger.Debug($"Vakuu: Low HP but can finish - boost offensive card {card.Title}");
                }
            }
        }
        
        return adjustedScore;
    }

    private int CalculateEnemyThreatLevel(EnemyCache[] enemyCache, GameStateInfo gameState)
    {
        int threatLevel = 0;
        
        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.Enemy == null || !ec.Enemy.IsAlive) continue;
            
            threatLevel += ec.ThreatScore;
            
            if (ec.IntendsToAttack)
            {
                threatLevel += 30;
            }
            
            if (ec.HasVulnerable || ec.HasWeak || ec.HasPoison)
            {
                threatLevel -= 10;
            }
        }
        
        if (gameState.EnemyCount > 1)
        {
            threatLevel = (int)(threatLevel * 1.3f);
        }
        
        return threatLevel;
    }

    private void LogBloodStateStrategy(BloodHealthState bloodState, float smoothFactor, CardModel selectedCard)
    {
        string stateStr = bloodState switch
        {
            BloodHealthState.Low => "LOW",
            BloodHealthState.Medium => "MEDIUM",
            BloodHealthState.High => "HIGH",
            _ => "UNKNOWN"
        };
        
        string priorityStr = GetCardPriorityLevel(selectedCard) switch
        {
            CardPriorityLevel.Survival => "SURVIVAL",
            CardPriorityLevel.Control => "CONTROL",
            CardPriorityLevel.Offensive => "OFFENSIVE",
            CardPriorityLevel.Utility => "UTILITY",
            _ => "UNKNOWN"
        };
        
        MainFile.Logger.Info($"Vakuu: BloodState={stateStr}, SmoothFactor={smoothFactor:F2}, " +
                            $"SelectedCard={selectedCard.Title}, Priority={priorityStr}");
    }

    private int CalculateEffectiveBlockNeeded(GameStateInfo gameState, int newBlockAmount)
    {
        int incomingDamage = gameState.IncomingDamage;
        int currentBlock = gameState.PlayerBlock + gameState.BlockPlayedThisTurn;
        
        if (incomingDamage <= 0)
        {
            int maxUsefulBlock = gameState.PlayerMaxHp / 4;
            int totalBlock = currentBlock + newBlockAmount;
            
            if (totalBlock > maxUsefulBlock)
            {
                return Math.Max(0, maxUsefulBlock - currentBlock);
            }
            
            return newBlockAmount;
        }
        
        int blockDeficit = incomingDamage - currentBlock;
        
        if (blockDeficit > 0)
        {
            int bufferBlock = gameState.PlayerMaxHp / 5;
            int optimalBlock = blockDeficit + bufferBlock;
            
            return Math.Min(newBlockAmount, optimalBlock);
        }
        
        int excessBlock = currentBlock - incomingDamage;
        int maxTotalBlock = incomingDamage + gameState.PlayerMaxHp / 3;
        int totalBlock2 = currentBlock + newBlockAmount;
        
        if (totalBlock2 > maxTotalBlock)
        {
            return Math.Max(0, maxTotalBlock - currentBlock);
        }
        
        return newBlockAmount;
    }
}
