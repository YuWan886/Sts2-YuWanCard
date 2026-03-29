# 示例

## 完整的模组示例

以下是一个完整的模组示例，展示了如何创建卡牌、遗物和能力。

### 使用 ConstructedCardModel（推荐）

`ConstructedCardModel` 提供更简洁的链式 API：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigStrike() : ConstructedCardModel(
    cost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> Vars => base.Vars
        .WithDamage(6);

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task UseCard(CardUser user, Creature? target, int stacks, CancellationToken ct)
    {
        await Damage(user, target, stacks);
    }
}

[Pool(typeof(ColorlessCardPool))]
public class PigPower() : ConstructedCardModel(
    cost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    protected override IEnumerable<DynamicVar> Vars => base.Vars
        .WithBlock(5)
        .WithPower<StrengthPower>(1);

    protected override IEnumerable<CardKeyword> Keywords => [CardKeyword.Exhaust];

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        RemoveKeyword(CardKeyword.Exhaust);
    }

    protected override async Task UseCard(CardUser user, Creature? target, int stacks, CancellationToken ct)
    {
        await Block(user, DynamicVars.Block.BaseValue);
        await PowerCmd.Apply<StrengthPower>(user.Creature, DynamicVars.Strength.BaseValue, user.Creature, this);
    }
}
```

### 模组入口文件

```csharp
// MainFile.cs
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("YuWanCard initialized");
    }
}
```

### 卡牌基类

```csharp
// YuWanCardModel.cs
using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Cards;

public abstract partial class YuWanCardModel(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://YuWanCard/images/card_portraits/{CardId}";

    public override string? CustomPortraitPath => $"{PortraitBasePath}.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
```

### 卡牌示例

```csharp
// PigHurt.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigHurt : YuWanCardModel
{
    public override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    public PigHurt() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.AllEnemies
    )
    {
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<VulnerablePower>()));
    }
    
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VulnerablePower>(CombatState!.HittableEnemies, 2, Owner.Creature, this);
    }

    public override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(2);
    }
}
```

### 能力基类

```csharp
// YuWanPowerModel.cs
using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Powers;

public abstract class YuWanPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/powers/{PowerId}.png";

    public override string? CustomPackedIconPath => IconBasePath;
    public override string? CustomBigIconPath => IconBasePath;
}
```

### 能力示例

```csharp
// PigDoubtPower.cs
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            int powerCount = Amount;

            for (int i = 0; i < powerCount; i++)
            {
                var randomPower = GetRandomPower();
                if (randomPower != null)
                {
                    await PowerCmd.Apply(randomPower.ToMutable(), Owner, 1, Owner, null);
                }
            }
        }
    }

    private PowerModel? GetRandomPower()
    {
        var rng = Owner.Player?.RunState.Rng;
        if (rng == null) return null;

        var filteredPowers = ModelDb.AllPowers
            .Where(p => !p.IsInstanced && IsSafePower(p))
            .ToList();

        if (filteredPowers.Count == 0) return null;

        return rng.Niche.NextItem(filteredPowers);
    }

    private bool IsSafePower(PowerModel power)
    {
        var unsafePowers = new HashSet<string>
        {
            "MegaCrit.Sts2.Core.Models.Powers.CurlUpPower",
            "MegaCrit.Sts2.Core.Models.Powers.AngryPower",
        };
        return !unsafePowers.Contains(power.GetType().FullName ?? "");
    }
}
```

### 遗物基类

```csharp
// YuWanRelicModel.cs
using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Relics;

public abstract class YuWanRelicModel : CustomRelicModel
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/relics/{RelicId}";

    public override string? PackedImagePath => $"{IconBasePath}.png";
    public override string? PackedOutlinePath => $"{IconBasePath}_outline.png";
}
```

## 多人游戏卡牌示例

### 仅限多人的卡牌

```csharp
// GiveYou.cs - 给予队友卡牌
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class GiveYou : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public GiveYou() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        var owner = Owner;

        var handCards = PileType.Hand.GetPile(owner).Cards
            .Where(c => c != this)
            .ToList();

        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCards = await CardSelectCmd.FromHand(choiceContext, owner, prefs, c => c != this, this);

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard != null)
        {
            int upgradeLevel = selectedCard.CurrentUpgradeLevel;
            await CardPileCmd.RemoveFromCombat(selectedCard);
            var newCard = CombatState!.CreateCard(selectedCard.CanonicalInstance, targetPlayer);
            for (int i = 0; i < upgradeLevel; i++)
            {
                CardCmd.Upgrade(newCard);
            }
            await CardPileCmd.AddGeneratedCardInCombat(newCard, PileType.Hand, addedByPlayer: true);
        }
    }

    public override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
        EnergyCost.UpgradeBy(-1);
    }
}
```

### 影响所有队友的卡牌

```csharp
// PigAngry.cs - 给所有队友力量
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigAngry : YuWanCardModel
{
    public override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(4m)];

    public PigAngry() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies
    )
    {
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<StrengthPower>()));
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<Creature> teammates = from c in CombatState!.GetTeammatesOf(Owner.Creature)
                                          where c != null && c.IsAlive && c.IsPlayer
                                          select c;

        await PowerCmd.Apply<StrengthPower>(teammates, DynamicVars.Strength.BaseValue, Owner.Creature, this);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Strength.UpgradeValueBy(2m);
    }
}
```

### 需要玩家选择同步的卡牌

```csharp
// ReviveKai.cs - 复活死亡队友
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class ReviveKai : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public ReviveKai() : base(
        baseCost: 4,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.None
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var deadPlayers = CombatState!.PlayerCreatures
            .Where(c => c.IsPlayer && c.IsDead)
            .ToList();

        if (deadPlayers.Count == 0)
        {
            MainFile.Logger.Warn("没有已死亡的队友，无法使用复活卡");
            return;
        }

        Creature? targetCreature;
        if (deadPlayers.Count == 1)
        {
            targetCreature = deadPlayers[0];
        }
        else
        {
            targetCreature = await SelectDeadPlayer(choiceContext, deadPlayers);
            if (targetCreature == null)
            {
                MainFile.Logger.Warn("未选择要复活的玩家");
                return;
            }
        }

        decimal healAmount = IsUpgraded
            ? targetCreature.MaxHp
            : targetCreature.MaxHp / 2m;

        await CreatureCmd.Heal(targetCreature, healAmount);

        var targetPlayer = targetCreature.Player;
        if (targetPlayer != null)
        {
            await RestorePlayerDeck(targetPlayer);
        }
    }

    private async Task RestorePlayerDeck(Player player)
    {
        if (player.PlayerCombatState == null) return;

        var cardsToAdd = new List<CardModel>();
        foreach (var deckCard in player.Deck.Cards)
        {
            var combatCard = CombatState!.CloneCard(deckCard);
            combatCard.DeckVersion = deckCard;
            cardsToAdd.Add(combatCard);
        }

        if (cardsToAdd.Count > 0)
        {
            await CardPileCmd.Add(cardsToAdd, PileType.Draw, CardPilePosition.Bottom, this, skipVisuals: true);
            player.PlayerCombatState.DrawPile.RandomizeOrderInternal(
                player,
                player.RunState.Rng.Shuffle,
                CombatState!
            );
        }
    }

    private async Task<Creature?> SelectDeadPlayer(PlayerChoiceContext choiceContext, List<Creature> deadPlayers)
    {
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(Owner);
        await choiceContext.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

        int selectedIndex;
        if (LocalContext.IsMe(Owner))
        {
            selectedIndex = await ShowDeadPlayerSelection(deadPlayers);
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                Owner,
                choiceId,
                PlayerChoiceResult.FromIndex(selectedIndex)
            );
        }
        else
        {
            selectedIndex = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(Owner, choiceId)).AsIndex();
        }

        await choiceContext.SignalPlayerChoiceEnded();

        if (selectedIndex < 0 || selectedIndex >= deadPlayers.Count)
        {
            return null;
        }

        return deadPlayers[selectedIndex];
    }

    private async Task<int> ShowDeadPlayerSelection(List<Creature> deadPlayers)
    {
        var targetManager = NTargetManager.Instance;
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature);
        var startPosition = creatureNode?.GlobalPosition ?? Vector2.Zero;

        targetManager.StartTargeting(
            TargetType.AnyPlayer,
            startPosition,
            TargetMode.ClickMouseToTarget,
            () => false,
            AllowTargetingDeadPlayer
        );

        var node = await targetManager.SelectionFinished();

        for (int i = 0; i < deadPlayers.Count; i++)
        {
            var deadPlayer = deadPlayers[i];
            if (node is NCreature nCreature && nCreature.Entity == deadPlayer)
            {
                return i;
            }
            if (node is NMultiplayerPlayerState nPlayerState && nPlayerState.Player.Creature == deadPlayer)
            {
                return i;
            }
        }

        return -1;
    }

    private bool AllowTargetingDeadPlayer(Node node)
    {
        if (node is NCreature nCreature)
        {
            return nCreature.Entity.IsPlayer && nCreature.Entity.IsDead;
        }
        if (node is NMultiplayerPlayerState nPlayerState)
        {
            return nPlayerState.Player.Creature.IsPlayer && nPlayerState.Player.Creature.IsDead;
        }
        return false;
    }
}
```

## 本地化文件示例

### 卡牌本地化 (cards.json)

```json
{
  "YUWANCARD-PIG_HURT.title": "猪受伤",
  "YUWANCARD-PIG_HURT.description": "给予所有敌人 {Vulnerable:diff()} 层 [red]易伤[/red]。"
}
```

### 能力本地化 (powers.json)

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪疑惑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "回合开始时，获得 {amount} 个随机能力。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "回合开始时，获得 {amount} 个随机能力。"
}
```

### 遗物本地化 (relics.json)

```json
{
  "YUWANCARD-RING_OF_SEVEN_CURSES.title": "七咒之戒",
  "YUWANCARD-RING_OF_SEVEN_CURSES.description": "获得 1 个药水栏位。[gold]+1[/gold] 能量。[gold]+1[/gold] 抽牌数。受伤 [red]+50%[/red]。打 BOSS [gold]+50%[/gold] 伤害，打小怪 [red]-25%[/red] 伤害。格挡 [red]-20%[/red]。获得金币 [red]-50%[/red]。每回合获得一张诅咒牌。休息处回复 [red]-50%[/red]。BOSS 战后失去 [red]25%[/red] 最大生命。",
  "YUWANCARD-RING_OF_SEVEN_CURSES.flavor": "七重诅咒，七重力量。",
  "YUWANCARD-RING_OF_SEVEN_CURSES.additionalRestSiteHealText": "七咒之戒：回复 {ActualHeal} 点生命"
}
```

## 自定义能量球示例

### 基础能量球

```csharp
using BaseLib.Abstracts;
using Godot;

namespace YuWanCard.Orbs;

public class PigOrb : CustomOrbModel
{
    public override string? CustomIconPath => "res://YuWanCard/images/orbs/pig_orb.png";
    public override string? CustomSpritePath => "res://YuWanCard/images/orbs/pig_orb_sprite.png";

    public override bool IncludeInRandomPool => true;

    public override string? CustomEvokeSfx => "res://YuWanCard/audio/orbs/pig_evoke.ogg";
    public override string? CustomChannelSfx => "res://YuWanCard/audio/orbs/pig_channel.ogg";
}
```

### 带自定义精灵的能量球

```csharp
using BaseLib.Abstracts;
using Godot;

namespace YuWanCard.Orbs;

public class AdvancedPigOrb : CustomOrbModel
{
    public override string? CustomIconPath => "res://YuWanCard/images/orbs/advanced_pig_orb.png";

    public override bool IncludeInRandomPool => false;

    public override Node2D? CreateCustomSprite()
    {
        var container = new Node2D();

        var mainSprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://YuWanCard/images/orbs/advanced_pig_main.png")
        };
        container.AddChild(mainSprite);

        var glowSprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://YuWanCard/images/orbs/advanced_pig_glow.png"),
            Modulate = new Color(1, 1, 1, 0.5f)
        };
        container.AddChild(glowSprite);

        return container;
    }
}
```

## 先古之民示例

### 自定义先古之民事件

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Ancients;

public class PigPig : CustomAncientModel
{
    private const string IconBasePath = "res://YuWanCard/images/ancients/pig_pig";

    private static readonly Regex PigCardPattern = new(@"^YUWANCARD-PIG_", RegexOptions.Compiled);

    public PigPig() : base(autoAdd: true)
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    private const string RunHistoryIconPath = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png";
    private const string RunHistoryIconOutlinePathStr = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png";
    
    private static Texture2D? _cachedRunHistoryIcon;
    private static Texture2D? _cachedRunHistoryIconOutline;
    
    public override string? CustomScenePath => "res://YuWanCard/scenes/ancients/pig_pig.tscn";
    public override string? CustomMapIconPath => $"{IconBasePath}.png";
    public override string? CustomMapIconOutlinePath => $"{IconBasePath}.png";
    
    public override Texture2D? CustomRunHistoryIcon
    {
        get
        {
            if (_cachedRunHistoryIcon == null)
            {
                _cachedRunHistoryIcon = GD.Load<Texture2D>(RunHistoryIconPath);
                if (_cachedRunHistoryIcon == null)
                {
                    MainFile.Logger.Warn($"Failed to load PigPig run history icon from {RunHistoryIconPath}");
                }
            }
            return _cachedRunHistoryIcon;
        }
    }
    
    public override Texture2D? CustomRunHistoryIconOutline
    {
        get
        {
            if (_cachedRunHistoryIconOutline == null)
            {
                _cachedRunHistoryIconOutline = GD.Load<Texture2D>(RunHistoryIconOutlinePathStr);
                if (_cachedRunHistoryIconOutline == null)
                {
                    MainFile.Logger.Warn($"Failed to load PigPig run history outline icon from {RunHistoryIconOutlinePathStr}");
                }
            }
            return _cachedRunHistoryIconOutline;
        }
    }

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        foreach (var path in base.GetAssetPaths(runState))
        {
            yield return path;
        }
        
        yield return RunHistoryIconPath;
        yield return RunHistoryIconOutlinePathStr;
        yield return CustomMapIconPath!;
    }

    private string FirstVisit => $"{Id.Entry}.talk.firstvisitEver.0-0.ancient";
    
    protected override AncientDialogueSet DefineDialogues()
    {
        var sfxPath = AncientDialogueUtil.SfxPath(FirstVisit);
        var firstVisit = new AncientDialogue(sfxPath);

        var characterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>();
        
        foreach (var character in ModelDb.AllCharacters)
        {
            var baseKey = AncientDialogueUtil.BaseLocKey(Id.Entry, character.Id.Entry);
            characterDialogues[character.Id.Entry] = AncientDialogueUtil.GetDialoguesForKey("ancients", baseKey);
        }
        
        return new AncientDialogueSet
        {
            FirstVisitEverDialogue = firstVisit,
            CharacterDialogues = characterDialogues,
            AgnosticDialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", AncientDialogueUtil.BaseLocKey(Id.Entry, "ANY"))
        };
    }

    protected override OptionPools MakeOptionPools => new(
        MakePool(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Circlet>()),
        MakePool(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Circlet>()),
        MakePool(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Circlet>())
    );

    public override IEnumerable<EventOption> AllPossibleOptions => [];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, ChoosePigCard, "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_CARD"),
            new(this, ChooseRelic, "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_RELIC"),
            new(this, UpgradeCards, "YUWANCARD-PIG_PIG.pages.INITIAL.options.UPGRADE_CARDS")
        };
    }

    private async Task ChoosePigCard()
    {
        var pigCards = GetPigCards();
        if (pigCards.Count == 0)
        {
            FinishEvent();
            return;
        }

        var shuffled = pigCards.OrderBy(_ => Rng.NextInt()).ToList();
        var cardsToOffer = shuffled.Take(Math.Min(5, shuffled.Count)).ToList();
        var cardReward = new CardReward(cardsToOffer, CardCreationSource.Other, Owner!);
        await RewardsCmd.OfferCustom(Owner!, [cardReward]);
        FinishEvent();
    }

    private async Task ChooseRelic()
    {
        List<RelicModel> relics = [];
        for (int i = 0; i < 3; i++)
        {
            var relic = RelicFactory.PullNextRelicFromFront(Owner!).ToMutable();
            relics.Add(relic);
        }
        var selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner!, relics);
        if (selectedRelic != null)
        {
            await RelicCmd.Obtain(selectedRelic, Owner!);
        }
        FinishEvent();
    }

    private async Task UpgradeCards()
    {
        var upgradeableCards = PileType.Deck.GetPile(Owner!).Cards
            .Where(c => c.IsUpgradable)
            .ToList();

        if (upgradeableCards.Count == 0)
        {
            FinishEvent();
            return;
        }

        var cardsToUpgrade = await CardSelectCmd.FromDeckForUpgrade(
            Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, Math.Min(5, upgradeableCards.Count))
        );

        foreach (var card in cardsToUpgrade)
        {
            CardCmd.Upgrade(card);
        }
        FinishEvent();
    }

    private List<CardModel> GetPigCards()
    {
        var colorlessPool = ModelDb.CardPool<ColorlessCardPool>();
        var allCards = colorlessPool.GetUnlockedCards(Owner!.UnlockState, Owner.RunState.CardMultiplayerConstraint);
        
        return [.. allCards
            .Where(c => PigCardPattern.IsMatch(c.Id.Entry))
            .Select(c => Owner.RunState.CreateCard(c, Owner))];
    }

    private void FinishEvent()
    {
        var doneMethod = typeof(AncientEventModel).GetMethod("Done", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doneMethod?.Invoke(this, null);
    }
}
```

**关键点说明**：
- 使用 `IsValidForAct` 指定出现的章节
- 使用 `DefineDialogues` 定义对话系统
- 使用 `OptionPools` 和 `AncientOption` 创建选项池
- 使用 `AncientDialogueUtil` 处理对话本地化
- 缓存纹理资源避免重复加载
- 使用 `GetAssetPaths` 注册资源路径

**本地化格式**：
```json
{
  "YUWANCARD-PIG_PIG.title": "猪猪",
  "YUWANCARD-PIG_PIG.epithet": "先古之民",
  "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_CARD.title": "选择卡牌",
  "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_RELIC.title": "选择遗物",
  "YUWANCARD-PIG_PIG.pages.INITIAL.options.UPGRADE_CARDS.title": "升级卡牌"
}
```

## 怪物示例

### 自定义怪物（精英怪）

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Monsters;

public sealed class Killer : MonsterModel
{
    public override string VisualsPath => "res://YuWanCard/scenes/monsters/killer/killer_visuals.tscn";

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 180);

    public override int MaxInitialHp => MinInitialHp;

    private static int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private static int MultiDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private static int MultiRepeat => 3;

    private static int ZoomDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private static int ZoomBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 10);

    private static int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private static int DazedCount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1);

    private static int HardenedShellAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 30);

    private static int PersonalHiveAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1);

    private static int SkittishAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 15);

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override string DeathSfx => "event:/sfx/enemy/enemy_attacks/hunter_killer/hunter_killer_die";

    private int _enlargeTriggers;

    public float CurrentScale { get; private set; } = 1f;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<HardenedShellPower>(Creature, HardenedShellAmount, Creature, null);
        foreach (var player in CombatState.Players)
        {
            player.Creature.Died += OnPlayerDied;
        }
    }

    private void OnPlayerDied(Creature creature)
    {
        creature.Died -= OnPlayerDied;
        if (!CombatState.Players.All(p => p.Creature.IsDead))
        {
            return;
        }
        LocString line = MonsterModel.L10NMonsterLookup("KILLER.onPlayerDeath.speakLine");
        TalkCmd.Play(line, Creature);
    }

    public override Task BeforeDeath(Creature creature)
    {
        if (creature != Creature)
        {
            return Task.CompletedTask;
        }
        LocString line = L10NMonsterLookup("KILLER.onDeath.speakLine");
        TalkCmd.Play(line, Creature);
        return Task.CompletedTask;
    }

    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = [];

        MoveState sleepMove = new("SLEEP_MOVE", SleepMove, new SleepIntent());
        MoveState wakeMove = new("WAKE_MOVE", WakeMove, new BuffIntent());
        MoveState slashMove = new("SLASH_MOVE", SlashMove, new SingleAttackIntent(SlashDamage));
        MoveState multiAttackMove = new("MULTI_ATTACK_MOVE", MultiAttackMove, new MultiAttackIntent(MultiDamage, MultiRepeat));
        MoveState goopMove = new("GOOP_MOVE", GoopMove, new DebuffIntent());
        MoveState zoomMove = new("ZOOM_MOVE", ZoomMove, new SingleAttackIntent(ZoomDamage), new DefendIntent());
        MoveState enlargeMove = new("ENLARGE_MOVE", EnlargeMove, new BuffIntent(), new StatusIntent(DazedCount));

        sleepMove.FollowUpState = wakeMove;
        wakeMove.FollowUpState = slashMove;

        RandomBranchState randomBranch = new("RAND");
        slashMove.FollowUpState = randomBranch;
        multiAttackMove.FollowUpState = randomBranch;
        goopMove.FollowUpState = randomBranch;
        zoomMove.FollowUpState = randomBranch;
        enlargeMove.FollowUpState = randomBranch;

        randomBranch.AddBranch(slashMove, MoveRepeatType.CannotRepeat);
        randomBranch.AddBranch(multiAttackMove, 2);
        randomBranch.AddBranch(goopMove, MoveRepeatType.CannotRepeat);
        randomBranch.AddBranch(zoomMove, 2);
        randomBranch.AddBranch(enlargeMove, 1);

        list.Add(sleepMove);
        list.Add(wakeMove);
        list.Add(slashMove);
        list.Add(multiAttackMove);
        list.Add(goopMove);
        list.Add(zoomMove);
        list.Add(enlargeMove);
        list.Add(randomBranch);

        return new MonsterMoveStateMachine(list, sleepMove);
    }

    private async Task SleepMove(IReadOnlyList<Creature> targets)
    {
        LocString line = L10NMonsterLookup("KILLER.moves.SLEEP.speakLine");
        ThinkCmd.Play(line, Creature);
        await Cmd.Wait(0.5f);
    }

    private async Task WakeMove(IReadOnlyList<Creature> targets)
    {
        if (TestMode.IsOff)
        {
            NRunMusicController.Instance?.TriggerEliteSecondPhase();
        }
        await PowerCmd.Apply<StrengthPower>(Creature, 8m, Creature, null);
        await PowerCmd.Apply<PersonalHivePower>(Creature, PersonalHiveAmount, Creature, null);
        await PowerCmd.Apply<SkittishPower>(Creature, SkittishAmount, Creature, null);
        LocString line = L10NMonsterLookup("KILLER.moves.WAKE.speakLine");
        TalkCmd.Play(line, Creature);
        await Cmd.Wait(0.5f);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(SlashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await Cmd.Wait(0.25f);
    }

    private async Task MultiAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MultiDamage).WithHitCount(MultiRepeat).OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task GoopMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.4f);
        await PowerCmd.Apply<TenderPower>(targets, 1m, Creature, null);
    }

    private async Task ZoomMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ZoomDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(Creature, ZoomBlock, ValueProp.Move, null);
    }

    private async Task EnlargeMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 1.0f);
        await PowerCmd.Apply<StrengthPower>(Creature, StrengthGain, Creature, null);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, DazedCount, addedByPlayer: false);
        _enlargeTriggers++;
        CurrentScale = 1f + 0.08f * _enlargeTriggers;
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.SetDefaultScaleTo(CurrentScale, 0.5f);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idleState = new("idle_loop", isLooping: true);
        AnimState castState = new("cast");
        AnimState attackState = new("attack");
        AnimState hurtState = new("hurt");
        AnimState dieState = new("die");

        castState.NextState = idleState;
        attackState.NextState = idleState;
        hurtState.NextState = idleState;

        CreatureAnimator animator = new(idleState, controller);
        animator.AddAnyState("Idle", idleState);
        animator.AddAnyState("Cast", castState);
        animator.AddAnyState("Attack", attackState);
        animator.AddAnyState("Dead", dieState);
        animator.AddAnyState("Hit", hurtState);

        return animator;
    }
}
```

**关键点说明**：
- 继承 `MonsterModel` 创建自定义怪物
- 使用 `AscensionHelper` 处理升阶难度
- 使用 `MonsterMoveStateMachine` 定义行为 AI
- 使用 `MonsterState` 定义各种行动
- 使用 `DamageCmd.Attack()` 创建攻击命令
- 使用 `PowerCmd.Apply()` 施加能力
- 实现 `GenerateAnimator()` 定义动画状态机
- 使用 `L10NMonsterLookup()` 获取本地化对话

**注册怪物**：
需要通过 Harmony 补丁将怪物注册到游戏中（参考 [KillerRegistrationPatch](file:///f:/sts2-mod/YuWanCard/YuWanCardCode/Patches/KillerRegistrationPatch.cs)）

**本地化格式**：
```json
{
  "KILLER.name": "杀手",
  "KILLER.moves.SLEEP.speakLine": "杀手在睡觉...",
  "KILLER.moves.WAKE.speakLine": "杀手醒了！",
  "KILLER.onDeath.speakLine": "不可能...",
  "KILLER.onPlayerDeath.speakLine": "你们都将成为我的手下败将！"
}
```

## 配置系统示例

### 完整配置类

```csharp
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace YuWanCard.Config;

[HoverTipsByDefault]
internal class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("日志设置")]
    public static bool OpenLogWindowOnStartup { get; set; } = false;

    [SliderRange(128, 2048, 64)]
    [SliderLabelFormat("{0:0}")]
    [ConfigHoverTip(false)]
    public static double LimitedLogSize { get; set; } = 256;

    [ConfigSection("游戏设置")]
    public static bool EnableSpecialEffects { get; set; } = true;

    [ConfigSection("难度设置")]
    public static DifficultyPreset Difficulty { get; set; } = DifficultyPreset.Normal;

    [SliderRange(0.5, 2.0, 0.1)]
    [SliderLabelFormat("{0:0.0}x")]
    [ConfigSection("数值调整")]
    public static double DamageMultiplier { get; set; } = 1.0;

    [SliderRange(-50, 50, 5)]
    [SliderLabelFormat("{0:+0;-0;0} HP")]
    [ConfigHoverTip(false)]
    public static double StartingHealthOffset { get; set; } = 0;

    [ConfigTextInput(TextInputPreset.SafeDisplayName, MaxLength = 16)]
    [ConfigHoverTip(false)]
    public static string PlayerNickname { get; set; } = "Player";

    [ConfigHideInUI]
    public static int TotalRunsPlayed { get; set; } = 0;

    [ConfigIgnore]
    public static int TemporaryCounter { get; set; } = 0;

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);

        optionContainer.AddChild(CreateDividerControl());

        var resetButton = CreateButton("ResetStats", "Reset", async () =>
        {
            var popup = NErrorPopup.Create(
                "重置统计",
                $"已重置 {TotalRunsPlayed} 次游戏记录。",
                false
            );
            if (popup != null && NModalContainer.Instance != null)
            {
                NModalContainer.Instance.Add(popup);
            }
            TotalRunsPlayed = 0;
        }, addHoverTip: false);
        optionContainer.AddChild(resetButton);

        AddRestoreDefaultsButton(optionContainer);
    }
}

public enum DifficultyPreset
{
    Easy,
    Normal,
    Hard,
    Nightmare
}
```

### 配置本地化 (settings_ui.json)

```json
{
  "YUWANCARD-LOG_SETTINGS.title": "日志设置",
  "YUWANCARD-GAME_SETTINGS.title": "游戏设置",
  "YUWANCARD-DIFFICULTY_SETTINGS.title": "难度设置",
  "YUWANCARD-NUMERIC_ADJUSTMENTS.title": "数值调整",

  "YUWANCARD-OPEN_LOG_WINDOW_ON_STARTUP.title": "启动时打开日志窗口",
  "YUWANCARD-OPEN_LOG_WINDOW_ON_STARTUP.hover.desc": "游戏启动时自动打开 BaseLib 日志窗口，方便调试。",

  "YUWANCARD-LIMITED_LOG_SIZE.title": "日志行数限制",

  "YUWANCARD-ENABLE_SPECIAL_EFFECTS.title": "启用特殊效果",
  "YUWANCARD-ENABLE_SPECIAL_EFFECTS.hover.desc": "启用模组的特殊视觉效果。",

  "YUWANCARD-DIFFICULTY.title": "难度预设",
  "YUWANCARD-DIFFICULTY.hover.desc": "选择游戏难度，影响敌人伤害和生命值。",

  "YUWANCARD-DAMAGE_MULTIPLIER.title": "伤害倍率",
  "YUWANCARD-DAMAGE_MULTIPLIER.hover.desc": "调整所有伤害的倍率。",

  "YUWANCARD-STARTING_HEALTH_OFFSET.title": "初始生命偏移",

  "YUWANCARD-PLAYER_NICKNAME.title": "玩家昵称",
  "YUWANCARD-PLAYER_NICKNAME.placeholder": "输入昵称...",

  "YUWANCARD-RESET_STATS.title": "重置统计",
  "YUWANCARD-RESET.title": "重置",

  "YUWANCARD-RESTORE_DEFAULTS.title": "恢复默认值"
}
```

### 注册配置

```csharp
// MainFile.cs
using BaseLib.Config;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public static void Initialize()
    {
        ModConfigRegistry.Register("YuWanCard", new YuWanCardConfig());
    }
}
```

### 在代码中使用配置

```csharp
using YuWanCard.Config;

public class MyCard : CustomCardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal damage = DynamicVars.Damage.BaseValue * (decimal)YuWanCardConfig.DamageMultiplier;
        await CommonActions.CardAttack(this, cardPlay.Target, damage, hitCount: 1);
    }
}

public class MyRelic : CustomRelicModel
{
    public override async Task AfterObtained()
    {
        if (YuWanCardConfig.EnableSpecialEffects)
        {
            // 执行特殊效果
        }
        await base.AfterObtained();
    }
}
```
