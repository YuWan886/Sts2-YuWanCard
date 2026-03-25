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
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    public PigHurt() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.AllEnemies
    )
    {
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VulnerablePower>(CombatState!.HittableEnemies, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade()
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
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(4m)];

    public PigAngry() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies
    )
    {
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
