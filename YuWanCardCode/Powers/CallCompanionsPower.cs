using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Godot;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class CallCompanionsPower : YuWanPowerModel
{
    private const int RandomCardCount = 20;
    private const int MaxCardsPerTurn = 10;

    private CharacterModel _character = null!;
    private readonly List<CardModel> _drawPile = [];
    private readonly List<CardModel> _hand = [];
    private readonly List<CardModel> _discard = [];
    private Creature? _companionCreature;
    private int _energy;
    private int _maxEnergy;
    private string _displayName = "";
    private int _companionIndex;
    private bool _initialized;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (_initialized) return;
        _initialized = true;

        var player = Owner!.Player!;
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        var rng = player.RunState.Rng.Shuffle;

        var allChars = ModelDb.AllCharacters.ToList();
        var available = allChars.Where(c => c.Id != player.Character.Id).ToList();
        if (available.Count == 0)
        {
            MainFile.Logger.Warn("CallCompanions: No other characters available");
            return;
        }

        _character = rng.NextItem(available)!;
        _maxEnergy = _character.MaxEnergy;
        _energy = _maxEnergy;

        foreach (var blueprint in _character.StartingDeck)
        {
            var card = blueprint.ToMutable();
            card.AddKeyword(CardKeyword.Ethereal);
            card.AddKeyword(CardKeyword.Exhaust);
            combatState.AddCard(card, player);
            _drawPile.Add(card);
        }

        var pool = _character.CardPool;
        var poolCards = pool.AllCards
            .Where(c => c.Rarity != CardRarity.Basic
                        && c.Rarity != CardRarity.Token
                        && c.Rarity != CardRarity.Status
                        && c.Rarity != CardRarity.Curse)
            .ToList();

        if (poolCards.Count > 0)
        {
            for (int i = 0; i < RandomCardCount; i++)
            {
                var blueprint = rng.NextItem(poolCards)!;
                var card = blueprint.ToMutable();
                card.AddKeyword(CardKeyword.Ethereal);
                card.AddKeyword(CardKeyword.Exhaust);
                combatState.AddCard(card, player);
                _drawPile.Add(card);
            }
        }

        rng.Shuffle(_drawPile);

        var playerId = player.Character.Id.Entry;
        var charId = _character.Id.Entry;
        _companionIndex = rng.NextInt(1, 100);
        _displayName = $"{playerId}_{charId}_{_companionIndex}";

        // Spawn visible companion creature with player-like HP
        await SpawnCompanionCreature(player);

        MainFile.Logger.Info($"CallCompanions: Summoned {_displayName} " +
            $"(character: {_character.Id}, HP: {_character.StartingHp}, deck: {_drawPile.Count} cards)");
        Flash();
    }

    private async Task SpawnCompanionCreature(Player player)
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        // Register the character's visual scene so it converts to NCreatureVisuals
        var visualPath = _character.VisualsPath;
        if (visualPath != null)
            NodeFactory.RegisterSceneType<NCreatureVisuals>(visualPath);

        // Set pending values before creating the mutable model
        CompanionPlaceholderModel.PendingVisualPath = visualPath;
        CompanionPlaceholderModel.PendingHp = _character.StartingHp;
        CompanionPlaceholderModel.PendingDisplayName = _displayName;
        var mutableModel = ModelDb.Monster<CompanionPlaceholderModel>().ToMutable();

        _companionCreature = combatState.CreateCreature(
            mutableModel, CombatSide.Player, slot: null);

        combatState.AddCreature(_companionCreature);
        player.PlayerCombatState?.AddPetInternal(_companionCreature);

        NCombatRoom.Instance?.AddCreature(_companionCreature);
        await CombatManager.Instance.AfterCreatureAdded(_companionCreature);

        // Set companion HP to character's starting HP
        await CreatureCompat.SetMaxAndCurrentHp(_companionCreature, _character.StartingHp);

        // Ensure health bar is visible on the creature node
        var companionNode = NCombatRoom.Instance?.GetCreatureNode(_companionCreature);
        companionNode?.ToggleIsInteractable(true);

        // Position like a second player
        PositionCompanionCreature(player.Creature);

        // Reposition existing pets so they don't overlap with the companion
        PetManager.PositionAllPets(player.Creature);
    }

    private void PositionCompanionCreature(Creature owner)
    {
        if (_companionCreature == null) return;

        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        var companionNode = NCombatRoom.Instance?.GetCreatureNode(_companionCreature);
        if (ownerNode == null || companionNode == null) return;

        float hitboxWidth = ownerNode.Hitbox.Size.X;
        float hitboxHeight = ownerNode.Hitbox.Size.Y;
        float horizontalSpacing = hitboxWidth + 20f;
        float verticalStagger = hitboxHeight * 0.3f;

        companionNode.Position = ownerNode.Position
            - new Vector2(horizontalSpacing, verticalStagger);
    }

    public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!_initialized || player.Creature != Owner) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        _energy = _maxEnergy;
        DrawCards(5, player.RunState.Rng.Shuffle);

        if (_hand.Count == 0) return;

        await ExecuteCompanionTurn(choiceContext, player, combatState);

        foreach (var card in _hand.ToList())
        {
            _hand.Remove(card);
            _discard.Add(card);
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_companionCreature is { IsAlive: true })
            await CreatureCmd.Kill(_companionCreature);

        _drawPile.Clear();
        _hand.Clear();
        _discard.Clear();
        _companionCreature = null;
        _initialized = false;
    }

    private void DrawCards(int count, Rng rng)
    {
        for (int i = 0; i < count; i++)
        {
            if (_drawPile.Count == 0)
            {
                if (_discard.Count == 0) break;
                foreach (var c in _discard)
                    _drawPile.Add(c);
                _discard.Clear();
                rng.Shuffle(_drawPile);
            }

            var drawn = _drawPile[^1];
            _drawPile.RemoveAt(_drawPile.Count - 1);
            _hand.Add(drawn);
        }
    }

    private async Task ExecuteCompanionTurn(
        PlayerChoiceContext choiceContext, Player player, CombatState combatState)
    {
        var state = AnalyzeState(player, combatState);
        var enemyCache = CacheEnemies(combatState);

        int cardsPlayed = 0;

        while (cardsPlayed < MaxCardsPerTurn)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;
            if (_hand.Count == 0 || _energy <= 0) break;

            var (card, target) = SelectBestCard(player, combatState, state, enemyCache);
            if (card == null) break;

            _hand.Remove(card);

            int cardCost = card.EnergyCost?.GetResolved() ?? 999;
            if (cardCost > _energy)
            {
                _discard.Add(card);
                continue;
            }

            int playerEnergy = player.PlayerCombatState?.Energy ?? 0;
            if (player.PlayerCombatState != null)
                player.PlayerCombatState.Energy = _energy;

            try
            {
                if (!combatState.ContainsCard(card))
                    combatState.AddCard(card, player);

                await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source: this, skipVisuals: true);
                await card.SpendResources();
                _energy = player.PlayerCombatState!.Energy;
                player.PlayerCombatState.Energy = playerEnergy;
                await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"CallCompanions: Error playing {card.Title}: {ex.Message}");
                if (player.PlayerCombatState != null)
                    player.PlayerCombatState.Energy = playerEnergy;
            }

            cardsPlayed++;
        }

        MainFile.Logger.Info(
            $"CallCompanions: {_displayName} played {cardsPlayed} cards, energy left: {_energy}");
    }

    private (CardModel? card, Creature? target) SelectBestCard(
        Player player, CombatState combatState, StateInfo state, EnemyCache[] enemyCache)
    {
        CardModel? bestCard = null;
        Creature? bestTarget = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i];
            int cost = card.EnergyCost?.GetResolved() ?? 999;
            if (cost > _energy) continue;

            int score = ScoreCard(card, state, enemyCache);
            if (score <= bestScore) continue;

            bestScore = score;
            bestCard = card;
            bestTarget = GetBestTarget(card, combatState, player, enemyCache);
        }

        return (bestCard, bestTarget);
    }

    private int ScoreCard(CardModel card, StateInfo state, EnemyCache[] enemyCache)
    {
        int score = 0;
        var vars = card.DynamicVars;
        if (vars == null) return -100;

        if (vars.TryGetValue("Damage", out var dmg) && dmg != null)
        {
            int val = (int)dmg.BaseValue;
            score += 60 + val * 5;
            for (int i = 0; i < enemyCache.Length; i++)
            {
                if (enemyCache[i].CurrentHp <= val)
                {
                    score += 80;
                    break;
                }
            }
            if (!state.EnemyIntendsAttack) score += 20;
            if (card.Tags.Contains(CardTag.Strike)) score += 30;
        }

        if (vars.TryGetValue("Block", out var blk) && blk != null)
        {
            score += 60;
            if (state.NeedsBlock)
            {
                score += 50;
                if ((int)blk.BaseValue >= state.IncomingDamage - state.PlayerBlock)
                    score += 30;
            }
            if (state.IsInDanger) score += 30;
            if (card.Tags.Contains(CardTag.Defend)) score += 20;
        }

        foreach (var kvp in vars)
        {
            if (IsBuffPower(kvp.Key))
            {
                score += 100;
                if (kvp.Key == "StrengthPower") score += 30 + state.EnemyCount * 10;
                if (kvp.Key == "DexterityPower" && state.NeedsBlock) score += 40;
                if (kvp.Key == "BufferPower" && state.IsInDanger) score += 50;
                if (kvp.Key == "IntangiblePower" && state.HpPercent < 40) score += 60;
            }

            if (IsDebuffPower(kvp.Key))
            {
                int needing = 0;
                for (int i = 0; i < enemyCache.Length; i++)
                    if (!HasDebuff(enemyCache[i], kvp.Key)) needing++;
                score += 80 + needing * 30;
            }
        }

        if (vars.TryGetValue("Cards", out var draw) && draw != null)
            score += 70 + (int)draw.BaseValue * 25;
        if (vars.TryGetValue("Energy", out var en) && en != null)
            score += 60 + (int)en.BaseValue * 30;
        if (vars.TryGetValue("Heal", out var heal) && heal != null)
            score += state.HpPercent < 50 ? 80 : 30;

        int cost = card.EnergyCost?.GetResolved() ?? 99;
        score -= cost * 15;

        return score;
    }

    private Creature? GetBestTarget(
        CardModel card, CombatState combatState, Player player, EnemyCache[] enemyCache)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.RandomEnemy => GetBestEnemyTarget(card, enemyCache),
            TargetType.AllEnemies => combatState.HittableEnemies.FirstOrDefault(),
            // Self targets the companion creature, not the summoner
            TargetType.Self => _companionCreature ?? player.Creature,
            TargetType.AnyAlly => combatState.Allies
                .Where(c => c != null && c.IsAlive && c.IsPlayer)
                .MinBy(c => (int)c.CurrentHp),
            _ => combatState.HittableEnemies.FirstOrDefault()
                ?? (Creature?)player.Creature
        };
    }

    private static Creature? GetBestEnemyTarget(CardModel card, EnemyCache[] enemyCache)
    {
        if (enemyCache.Length == 0) return null;

        var vars = card.DynamicVars;
        int damage = 0;
        if (vars != null && vars.TryGetValue("Damage", out var dmg) && dmg != null)
            damage = (int)dmg.BaseValue;

        EnemyCache? best = null;
        for (int i = 0; i < enemyCache.Length; i++)
        {
            var ec = enemyCache[i];
            if (ec.Enemy == null) continue;

            if (best == null) best = ec;

            if (damage > 0 && ec.CurrentHp <= damage && (
                best.CurrentHp > damage || (ec.IntendsToAttack && !best.IntendsToAttack)))
                best = ec;
            if (!best.IntendsToAttack && ec.IntendsToAttack)
                best = ec;
        }

        return best?.Enemy;
    }

    private static bool IsBuffPower(string name) => name switch
    {
        "StrengthPower" or "DexterityPower" or "FocusPower" or "ArtifactPower"
            or "BufferPower" or "IntangiblePower" or "RegenPower" or "ThornsPower"
            or "VigorPower" or "AccuracyPower" or "RagePower" or "PlatingPower" => true,
        _ => false
    };

    private static bool IsDebuffPower(string name) => name switch
    {
        "VulnerablePower" or "WeakPower" or "PoisonPower" or "DebilitatePower"
            or "EntanglePower" or "SlowPower" or "DoomPower" => true,
        _ => false
    };

    private static bool HasDebuff(EnemyCache ec, string powerName) => powerName switch
    {
        "VulnerablePower" => ec.HasVulnerable,
        "WeakPower" => ec.HasWeak,
        "DebilitatePower" => ec.HasDebilitate,
        "PoisonPower" => ec.HasPoison,
        _ => false
    };

    private StateInfo AnalyzeState(Player player, CombatState combatState)
    {
        var info = new StateInfo
        {
            PlayerHp = (int)player.Creature.CurrentHp,
            PlayerMaxHp = (int)player.Creature.MaxHp,
            PlayerBlock = (int)player.Creature.Block,
            IncomingDamage = CalcIncomingDamage(combatState),
            EnemyCount = combatState.HittableEnemies.Count,
            HpPercent = player.Creature.MaxHp > 0
                ? (int)(player.Creature.CurrentHp * 100 / player.Creature.MaxHp)
                : 100
        };

        var enemies = combatState.HittableEnemies;
        info.IsInDanger = info.HpPercent < 30 ||
            (info.IncomingDamage > info.PlayerHp - info.PlayerBlock);
        info.NeedsBlock = info.IncomingDamage > info.PlayerBlock && info.IncomingDamage > 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].Monster?.IntendsToAttack == true)
            {
                info.EnemyIntendsAttack = true;
                break;
            }
        }

        return info;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalcIncomingDamage(CombatState combatState)
    {
        int total = 0;
        for (int i = 0; i < combatState.Enemies.Count; i++)
        {
            var creature = combatState.Enemies[i];
            if (creature is not { IsAlive: true }) continue;

            var monster = creature.Monster;
            if (monster == null) continue;

            foreach (var intent in monster.NextMove.Intents)
            {
                if (intent is AttackIntent attack)
                    total += attack.GetTotalDamage(combatState.PlayerCreatures, creature);
            }
        }
        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static EnemyCache[] CacheEnemies(CombatState combatState)
    {
        var enemies = combatState.HittableEnemies;
        var cache = new EnemyCache[enemies.Count];
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            cache[i] = new EnemyCache
            {
                Enemy = e,
                CurrentHp = (int)e.CurrentHp,
                IntendsToAttack = e.Monster?.IntendsToAttack == true,
                HasVulnerable = e.HasPower<VulnerablePower>(),
                HasWeak = e.HasPower<WeakPower>(),
                HasDebilitate = e.HasPower<DebilitatePower>(),
                HasPoison = e.HasPower<PoisonPower>()
            };
        }
        return cache;
    }

    private sealed class EnemyCache
    {
        public Creature Enemy = null!;
        public int CurrentHp;
        public bool IntendsToAttack;
        public bool HasVulnerable;
        public bool HasWeak;
        public bool HasDebilitate;
        public bool HasPoison;
    }

    private sealed class StateInfo
    {
        public int PlayerHp;
        public int PlayerMaxHp;
        public int PlayerBlock;
        public int IncomingDamage;
        public bool EnemyIntendsAttack;
        public int EnemyCount;
        public int HpPercent;
        public bool IsInDanger;
        public bool NeedsBlock;
    }
}
