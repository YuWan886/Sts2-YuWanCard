# 核心功能

## 自定义卡牌

BaseLib 提供两种方式定义自定义卡牌：`CustomCardModel`（传统方式）和 `ConstructedCardModel`（链式 API 方式）。

### ConstructedCardModel（推荐）

`ConstructedCardModel` 是 BaseLib 新增的替代性卡牌基类，提供流畅的链式 API 来构建卡牌属性，大幅简化卡牌定义：

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyCard : ConstructedCardModel
{
    public MyCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }
}
```

#### 链式 API 方法

| 方法 | 说明 |
|------|------|
| `WithVars(params DynamicVar[] vars)` | 添加多个动态变量 |
| `WithVar(string name, int baseVal, int upgrade = 0)` | 添加命名变量 |
| `WithBlock(int baseVal, int upgrade = 0)` | 生成 BlockVar |
| `WithDamage(int baseVal, int upgrade = 0)` | 生成 DamageVar |
| `WithCards(int baseVal, int upgrade = 0)` | 生成 CardsVar |
| `WithPower<T>(int baseVal, int upgrade = 0)` | 生成 PowerVar<T> 并添加工具提示 |
| `WithPower<T>(string name, int baseVal, int upgrade = 0)` | 带名称的 PowerVar |
| `WithTags(params CardTag[] tags)` | 添加卡牌标签 |
| `WithKeywords(params CardKeyword[] keywords)` | 添加关键词 |
| `WithTip(TooltipSource tipSource)` | 添加工具提示 |
| `WithEnergyTip()` | 添加能量提示 |

#### 计算变量方法

用于动态计算伤害或格挡值：

| 方法 | 说明 |
|------|------|
| `WithCalculatedVar(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, ...)` | 添加计算变量（baseVal + bonus） |
| `WithCalculatedVar(string name, int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ...)` | 添加计算变量（baseVal + multVal * mult） |
| `WithCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus, ...)` | 添加计算格挡（变量名为 CalculatedBlock） |
| `WithCalculatedBlock(int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ...)` | 添加计算格挡（baseVal + multVal * mult） |
| `WithCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus, ...)` | 添加计算伤害（变量名为 CalculatedDamage） |
| `WithCalculatedDamage(int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ...)` | 添加计算伤害（baseVal + multVal * mult） |

**计算变量示例**：

```csharp
// 基础伤害 + 额外伤害（基于目标已损失生命值）
WithCalculatedDamage(6, (card, target) => 
    target != null ? Math.Floor((target.MaxHp - target.CurrentHp) / 10m) : 0);

// 基础格挡 + 倍率计算（连击数 * 2）
WithCalculatedBlock(5, 2, (card, target) => 
    card.Owner.Creature.CombatState?.TurnCardsPlayed ?? 0);
```

**注意**：每张卡牌目前只支持一个计算变量。

#### 完整示例

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class StrikeCard : ConstructedCardModel
{
    public StrikeCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }
}

[Pool(typeof(ColorlessCardPool))]
public class DefendCard : ConstructedCardModel
{
    public DefendCard() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithBlock(5);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.None, null);
    }
}
```

### CustomCardModel（传统方式）

继承 `CustomCardModel` 来创建自定义卡牌：

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyCustomCard : CustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m)];

    public MyCustomCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy
    )
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);
    }
}
```

### 构造函数参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `baseCost` | int | 基础费用 |
| `type` | CardType | 卡牌类型（Attack、Skill、Power、Status、Curse、Quest） |
| `rarity` | CardRarity | 稀有度（Common、Uncommon、Rare、Ancient） |
| `target` | TargetType | 目标类型 |
| `showInCardLibrary` | bool | 是否在卡牌库中显示（默认 true） |
| `autoAdd` | bool | 是否自动添加到内容字典（默认 true） |

### TargetType 目标类型

| TargetType | 说明 |
|------------|------|
| `TargetType.Self` | 自身 |
| `TargetType.AllAllies` | 所有队友（包括自己） |
| `TargetType.AnyAlly` | 任意队友（包括自己） |
| `TargetType.AllEnemies` | 所有敌人 |
| `TargetType.AnyEnemy` | 任意敌人 |
| `TargetType.RandomEnemy` | 随机敌人 |
| `TargetType.AnyPlayer` | 任意玩家（可用于选择死亡玩家） |
| `TargetType.None` | 无目标 |

### 重要属性和方法

| 属性/方法 | 说明 |
|-----------|------|
| `GainsBlock` | 自动检测卡牌是否有格挡效果 |
| `CustomFrame` | 自定义卡牌框贴图（可选） |
| `CustomFrameMaterial` | 自定义卡牌框材质（可选，延迟初始化） |
| `CreateCustomFrameMaterial` | 创建自定义 ShaderMaterial（可选，用于每张卡牌独立的材质） |
| `CustomPortraitPath` | 自定义卡牌立绘路径（可选） |
| `CanonicalVars` | 定义卡牌的动态变量 |
| `OnPlay` | 卡牌打出时的逻辑 |
| `OnUpgrade` | 卡牌升级时的逻辑 |
| `MultiplayerConstraint` | 多人游戏限制（默认 `CardMultiplayerConstraint.None`） |

### 自定义卡牌框材质

使用 `CreateCustomFrameMaterial` 属性为单张卡牌创建自定义 ShaderMaterial：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;

public class MySpecialCard : CustomCardModel
{
    public override Material? CreateCustomFrameMaterial => ShaderUtils.GenerateHsv(0.5f, 0.8f, 1.0f);
}
```

**说明**：
- `CustomFrameMaterial` 是延迟初始化的属性，会调用 `CreateCustomFrameMaterial` 并缓存结果
- 使用 `ShaderUtils.GenerateHsv(h, s, v)` 可以快速生成 HSV 着色器材质
- 此功能允许每张卡牌拥有独立的材质，而不是共享卡牌池的材质

### CardMultiplayerConstraint 多人游戏限制

| 值 | 说明 |
|----|------|
| `CardMultiplayerConstraint.None` | 无限制（默认值），单人/多人模式都会出现 |
| `CardMultiplayerConstraint.MultiplayerOnly` | 仅限多人游戏，单人模式不会出现 |

### DynamicVar 常用类型

| 类型 | 说明 |
|------|------|
| `DamageVar(decimal)` | 伤害变量 |
| `BlockVar(decimal, ValueProp)` | 格挡变量 |
| `HealVar(decimal)` | 治疗变量 |
| `EnergyVar(decimal)` | 能量变量 |
| `PowerVar<TPower>(decimal)` | 能力层数变量 |
| `CardsVar(decimal)` | 卡牌数量变量 |
| `HpLossVar(decimal)` | 生命损失变量 |
| `MaxHpVar(decimal)` | 最大生命值变量 |
| `GoldVar(decimal)` | 金币值变量 |
| `IntVar(string, decimal)` | 简单整数变量 |
| `RepeatVar(decimal)` | 重复次数变量 |
| `CalculatedDamageVar(decimal, decimal, decimal)` | 计算伤害（基础+额外*倍率） |
| `CalculatedBlockVar(decimal, decimal, decimal)` | 计算格挡 |

### CardKeyword 常用类型

| 关键词 | 说明 |
|--------|------|
| `CardKeyword.Exhaust` | 消耗 |
| `CardKeyword.Innate` | 固有 |
| `CardKeyword.Ethereal` | 虚无 |
| `CardKeyword.Retain` | 保留 |

使用 `CanonicalKeywords` 属性返回卡牌关键词，使用 `AddKeyword(CardKeyword)` 和 `RemoveKeyword(CardKeyword)` 方法在升级时修改关键词。

### ExtraHoverTips 能力提示显示

使用 `WithTip()` 方法在构造函数中添加能力提示：

```csharp
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : CustomCardModel
{
    public RainDark() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies
    )
    {
        WithPower<IntangiblePower>(3);
        WithPower<RainDarkPower>(3);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<IntangiblePower>()));
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<RainDarkPower>()));
    }
}
```

**注意**：`ExtraHoverTips` 属性在 `ConstructedCardModel` 中是密封的，无法重写。请使用 `WithTip()` 方法代替。

**常用 HoverTips 类型**：

| 方法 | 说明 |
|------|------|
| `HoverTipFactory.FromPower<TPower>()` | 显示能力介绍 |
| `HoverTipFactory.Static(StaticHoverTip.Block)` | 显示格挡图标 |
| `HoverTipFactory.FromKeyword(CardKeyword)` | 显示关键词介绍 |
| `base.EnergyHoverTip` | 显示能量图标（继承自基类） |
| `WithEnergyTip()` | 在构造函数中添加能量提示 |

**完整示例**：

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : ConstructedCardModel
{
    private const float HpPercentage = 0.25f;

    public RainDark() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies)
    {
        WithPower<IntangiblePower>(3);
        WithPower<RainDarkPower>(3);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer)
            .ToList();

        foreach (var teammate in teammates)
        {
            int targetHp = (int)(teammate.MaxHp * HpPercentage);
            await CreatureCmd.SetCurrentHp(teammate, targetHp);

            await CommonActions.Apply<IntangiblePower>(teammate, this, DynamicVars["IntangiblePower"].IntValue);
            await CommonActions.Apply<RainDarkPower>(teammate, this, DynamicVars["RainDarkPower"].IntValue);

            var player = teammate.Player;
            if (player != null && player.PlayerCombatState != null)
            {
                int currentEnergy = player.PlayerCombatState.Energy;
                if (currentEnergy > 0)
                {
                    await PlayerCmd.GainEnergy(currentEnergy, player);
                }
            }
        }
    }
}
```

**注意事项**：
- 使用 `WithTip()` 方法在构造函数中添加悬浮提示
- 对于给予能力的卡牌，强烈建议添加对应的能力 HoverTips
- 对于包含格挡、能量等效果的卡牌，可以使用 `WithEnergyTip()` 添加能量提示
- HoverTips 会按照添加顺序依次显示

## 自定义角色 (CustomCharacterModel)

继承 `CustomCharacterModel` 来创建自定义角色：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;

public class MyCustomCharacter : CustomCharacterModel
{
    public MyCustomCharacter()
    {
        Name = "My Character";
        StartingHealth = 70;
        StartingGold = 99;
        AttackAnimDelay = 0.15f;
        CastAnimDelay = 0.25f;
    }

    public override string? CustomVisualPath => "res://scenes/creature_visuals/my_character.tscn";
    public override string? CustomIconPath => "res://textures/icons/my_character.png";
}
```

### 视觉资源属性

| 属性 | 说明 |
|------|------|
| `CustomVisualPath` | 角色视觉场景路径（默认查找 `res://scenes/creature_visuals/class_name.tscn`） |
| `CustomTrailPath` | 卡牌轨迹路径 |
| `CustomIconTexturePath` | 小图标（保存运行信息弹窗用） |
| `CustomIconPath` | 左上角图标和图鉴筛选图标 |
| `CustomEnergyCounterPath` | 能量计数器场景路径 |
| `CustomRestSiteAnimPath` | 休息处动画路径 |
| `CustomMerchantAnimPath` | 商人动画路径 |
| `CustomCharacterSelectBg` | 角色选择背景场景 |
| `CustomCharacterSelectIconPath` | 角色选择图标 |
| `CustomCharacterSelectLockedIconPath` | 角色选择锁定图标 |
| `CustomMapMarkerPath` | 地图标记路径 |
| `CustomAttackSfx` | 攻击音效 |
| `CustomCastSfx` | 施法音效 |
| `CustomDeathSfx` | 死亡音效 |

### 默认值

| 属性 | 默认值 |
|------|--------|
| `StartingGold` | 99 |
| `AttackAnimDelay` | 0.15f |
| `CastAnimDelay` | 0.25f |

### 视觉场景要求

- 如果不重写 `CustomVisualPath`，则需要在 `res://scenes/creature_visuals/` 目录下创建名为 `class_name.tscn` 的场景文件
- 角色选择背景场景路径：`res://scenes/screens/char_select/char_select_bg_class_name.tscn`
- 场景必须包含以下节点：`Visuals`、`Bounds`、`IntentPos`、`CenterPos`、`OrbPos`、`TalkPos`

### 自定义能量计数器

可以使用 `CustomEnergyCounter` 结构体来自定义能量计数器的外观：

```csharp
public override CustomEnergyCounter? CustomEnergyCounter => new(
    layer => $"res://textures/energy/my_character_energy_layer{layer}.png",
    outlineColor: new Color(1, 0, 0),
    burstColor: new Color(1, 0.5f, 0)
);
```

`CustomEnergyCounter` 构造函数参数：
- `pathFunc`：`Func<int, string>` - 根据层数（1-5）返回对应贴图路径
- `outlineColor`：`Color` - 轮廓颜色
- `burstColor`：`Color` - 能量爆发粒子颜色

### SetupAnimationState 静态方法

`SetupAnimationState` 是一个静态辅助方法，用于简化动画状态设置：

```csharp
public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
{
    return SetupAnimationState(
        controller,
        idleName: "idle",           // 待机动画名称
        deadName: "dead",           // 死亡动画名称
        deadLoop: false,            // 死亡动画是否循环
        hitName: "hit",             // 受击动画名称
        hitLoop: false,             // 受击动画是否循环
        attackName: "attack",       // 攻击动画名称
        attackLoop: false,          // 攻击动画是否循环
        castName: "cast",           // 施法动画名称
        castLoop: false,            // 施法动画是否循环
        relaxedName: "relaxed",     // 放松动画名称
        relaxedLoop: true           // 放松动画是否循环
    );
}
```

### CreateCustomVisuals 方法

可以重写此方法创建自定义视觉组件：

```csharp
public override NCreatureVisuals? CreateCustomVisuals()
{
    if (CustomVisualPath == null) return null;
    return GodotUtils.CreatureVisualsFromScene(CustomVisualPath);
}
```

## 自定义遗物 (CustomRelicModel)

继承 `CustomRelicModel` 来创建自定义遗物：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

[Pool(typeof(SharedRelicPool))]
public class MyCustomRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
    }
}
```

### 遗物稀有度

| 稀有度 | 说明 |
|--------|------|
| `RelicRarity.Common` | 普通 |
| `RelicRarity.Uncommon` | 罕见 |
| `RelicRarity.Rare` | 稀有 |
| `RelicRarity.Ancient` | 先古之民 |
| `RelicRarity.Shop` | 商店 |

### 常用钩子方法

| 方法 | 说明 |
|------|------|
| `AfterObtained()` | 获得遗物时触发 |
| `AfterPlayerTurnStart(PlayerChoiceContext, Player)` | 玩家回合开始时触发 |
| `AfterCombatVictory(CombatRoom)` | 战斗胜利后触发 |
| `ModifyDamageMultiplicative(Creature?, decimal, ValueProp, Creature?, CardModel?)` | 修改伤害倍率 |
| `ModifyBlockMultiplicative(Creature, decimal, ValueProp, CardModel?, CardPlay?)` | 修改格挡倍率 |
| `ModifyMaxEnergy(Player, decimal)` | 修改最大能量 |
| `ModifyHandDraw(Player, decimal)` | 修改抽牌数 |
| `ModifyRestSiteHealAmount(Creature, decimal)` | 修改休息处回复血量 |
| `ModifyExtraRestSiteHealText(Player, IReadOnlyList<LocString>)` | 修改休息处额外文本 |
| `TryModifyRewards(Player, List<Reward>, AbstractRoom?)` | 修改奖励 |
| `ShouldGainGold(decimal, Player)` | 获得金币前触发 |
| `AfterGoldGained(Player)` | 获得金币后触发 |
| `ShouldAllowSelectingMoreCardRewards(Player)` | 是否允许选择更多卡牌奖励 |
| `GetUpgradeReplacement()` | 返回升级替换的遗物（默认 null） |

### 遗物升级替换

遗物可以设置升级替换（被 Orobas 先古之民遗物升级事件升级时）：

```csharp
public override RelicModel? GetUpgradeReplacement()
{
    return new MyUpgradedRelic();
}
```

**注意**：升级后的遗物也需要使用 `[Pool]` 属性标记，并且应该有不同的 ID。

**完整遗物示例**：

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class RingOfSevenCurses : CustomRelicModel
{
    private static readonly HashSet<Type> CurseBlacklist =
    [
        typeof(Normality),
        typeof(Enthralled),
        typeof(BadLuck)
    ];

    private GoldModificationGuard? _goldGuard;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
        () => Owner,
        amount => Math.Floor(amount * 0.5m),
        async amount => await PlayerCmd.LoseGold(amount, Owner!)
    );

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        return GoldGuard.ShouldGainGold(amount, player);
    }

    public override async Task AfterGoldGained(Player player)
    {
        await GoldGuard.AfterGoldGained(player);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        
        var availableCurses = ModelDb.CardPool<CurseCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => !CurseBlacklist.Contains(c.GetType()))
            .ToHashSet();
        if (availableCurses.Count == 0) return;

        Flash();
        CardModel? curseCard = Owner.RunState.Rng.Niche.NextItem(availableCurses);
        if (curseCard == null || Owner.Creature.CombatState == null) return;
        
        CardModel card = Owner.Creature.CombatState.CreateCard(curseCard, Owner);
        await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, addedByPlayer: true);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != null && target.Player == Owner) return 1.5m;
        if (dealer != null && dealer.Player == Owner)
        {
            if (target != null && target.CombatState?.Encounter?.RoomType == RoomType.Boss) return 1.5m;
            return 0.75m;
        }
        return 1m;
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target.Player == Owner) return 0.8m;
        return 1m;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + 1 : amount;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return player == Owner ? count + 1 : count;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room == null) return false;

        if (room.RoomType == RoomType.Monster || room.RoomType == RoomType.Boss || room.RoomType == RoomType.Elite)
        {
            rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
        }
        if (room.RoomType == RoomType.Monster && Owner.RunState.Rng.Niche.NextFloat() <= 0.7f)
        {
            rewards.Add(new RelicReward(player));
        }
        return true;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        return (creature.Player == Owner || creature.PetOwner == Owner) ? amount * 0.75m : amount;
    }

    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText)
    {
        if (player != Owner) return currentExtraText;
        var list = new List<LocString>(currentExtraText);
        var extraText = new LocString("relics", "YUWANCARD-RING_OF_SEVEN_CURSES.additionalRestSiteHealText");
        list.Add(extraText);
        return list;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (room == null || room.RoomType != RoomType.Boss || Owner == null) return;
        int maxHpLoss = (int)Math.Floor(Owner.Creature.MaxHp * 0.25m);
        if (maxHpLoss > 0)
        {
            Flash();
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, maxHpLoss, isFromCard: false);
        }
    }
}
```

## 自定义能力 (CustomPowerModel)

继承 `CustomPowerModel` 来创建自定义能力：

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigDoubtPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PigDoubtPower", 1)];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            int powerCount = Amount;

            for (int i = 0; i < powerCount; i++)
            {
                if (CombatManager.Instance.IsEnding)
                {
                    break;
                }

                var randomPower = GetRandomPower();
                if (randomPower != null)
                {
                    await PowerCmd.Apply(randomPower.ToMutable(), Owner, 1, Owner, null);
                }

                if (await CombatManager.Instance.CheckWinCondition())
                {
                    break;
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
        if (power is YuWanPowerModel)
        {
            return false;
        }

        return PowerSafetyUtils.IsSafePower(power);
    }
}
```

**图标路径**：如果继承 `YuWanPowerModel`，图标路径会根据 PowerId 自动生成：
- 格式：`res://YuWanCard/images/powers/{power_id}.png`
- 例如：`PigDoubtPower` → `pig_doubt_power` → `res://YuWanCard/images/powers/pig_doubt_power.png`

如果需要自定义图标路径，可以重写：
```csharp
public override string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png";
public override string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";
```

### 图标尺寸

| 属性 | 尺寸 |
|------|------|
| `CustomPackedIconPath` | 64x64 像素 |
| `CustomBigIconPath` | 256x256 像素 |
| `CustomBigBetaIconPath` | 256x256 像素（Beta 版本图标） |

### PowerType 类型

| 类型 | 说明 |
|------|------|
| `PowerType.Buff` | 增益效果（绿色） |
| `PowerType.Debuff` | 减益效果（红色） |
| `PowerType.Neutral` | 中性效果（蓝色） |

### PowerStackType 类型

| 类型 | 说明 |
|------|------|
| `PowerStackType.Counter` | 层数叠加 |
| `PowerStackType.Duration` | 持续时间 |
| `PowerStackType.None` | 不叠加 |

### 常用事件方法

| 方法 | 说明 |
|------|------|
| `AfterSideTurnStart(CombatSide, CombatState)` | 回合开始时触发 |
| `AfterSideTurnEnd(CombatSide, CombatState)` | 回合结束时触发 |
| `OnApply(Creature, int)` | 能力被应用时触发 |
| `OnRemove()` | 能力被移除时触发 |
| `Flash()` | 闪烁能力图标（视觉反馈） |

### ICustomPower 接口

如果你的能力需要继承自其他能力类（而不是直接继承 `PowerModel`），可以实现 `ICustomPower` 接口：

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

public class MyCustomPower : SomeOtherPower, ICustomPower
{
    public string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png";
    public string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";
    public string? CustomBigBetaIconPath => null;
}
```

**ICustomPower 接口属性**：

| 属性 | 说明 |
|------|------|
| `CustomPackedIconPath` | 小图标路径（64x64 像素） |
| `CustomBigIconPath` | 大图标路径（256x256 像素） |
| `CustomBigBetaIconPath` | Beta 版本大图标路径（256x256 像素，可选） |

**说明**：
- `CustomPowerModel` 同时继承了 `PowerModel` 和 `ICustomPower`，适合大多数情况
- `ICustomPower` 接口适合需要继承其他能力类的情况
- 实现此接口时，必须提供至少 `CustomPackedIconPath` 和 `CustomBigIconPath`

### Health Bar Forecast（生命条预测）

BaseLib v0.2.8 新增了生命条预测系统，允许能力在生物的生命条上显示预测效果（如毒素伤害、毁灭效果等）。

`CustomPowerModel` 默认实现了 `IHealthBarForecastSource` 接口，只需重写 `GetHealthBarForecastSegments` 方法即可：

```csharp
using BaseLib.Hooks;
using Godot;

public class MyPoisonPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (context.Creature == Owner && Amount > 0)
        {
            yield return new HealthBarForecastSegment(
                Amount,
                new Color(0.5f, 0.2f, 0.8f),
                HealthBarForecastDirection.FromRight,
                Order: 0
            );
        }
    }
}
```

#### HealthBarForecastSegment 结构

| 参数 | 类型 | 说明 |
|------|------|------|
| `Amount` | `int` | 预测的 HP 变化量 |
| `Color` | `Color` | 预测条颜色，也用于致死时的 HP 标签颜色 |
| `Direction` | `HealthBarForecastDirection` | 预测条生长方向 |
| `Order` | `int` | 渲染顺序，较小的值先渲染 |
| `OverlayMaterial` | `Material?` | 可选的覆盖材质（如毁灭条着色器） |
| `OverlaySelfModulate` | `Color?` | 可选的覆盖层自调制颜色 |

#### HealthBarForecastDirection 方向

| 值 | 说明 |
|----|------|
| `FromRight` | 从当前 HP 边缘向内生长（如毒素效果） |
| `FromLeft` | 从空白侧向外生长（如毁灭效果） |

#### HealthBarForecastOrder 辅助方法

用于计算回合相关的渲染顺序：

```csharp
// 回合开始时效果
int order = HealthBarForecastOrder.ForSideTurnStart(creature, triggerSide);

// 回合结束时效果
int order = HealthBarForecastOrder.ForSideTurnEnd(creature, triggerSide);
```

#### 使用 Shader 材质

结合 `ShaderUtils.CreateDoomBarShaderMaterial` 创建类似毁灭效果的预测条：

```csharp
public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
{
    if (Amount > 0)
    {
        var material = ShaderUtils.CreateDoomBarShaderMaterial(
            ShaderUtils.CreateVanillaDoomBarGradientTexture()
        );
        
        yield return new HealthBarForecastSegment(
            Amount,
            new Color(0.8f, 0.3f, 0.5f),
            HealthBarForecastDirection.FromLeft,
            Order: 0,
            OverlayMaterial: material
        );
    }
}
```

#### HealthBarForecastRegistry 注册外部来源

除了能力外，还可以注册外部的预测来源：

```csharp
using BaseLib.Hooks;

// 注册类型化来源
HealthBarForecastRegistry.Register<MyForecastSource>("MyMod", "MySource");

// 注册实例
HealthBarForecastRegistry.Register("MyMod", "MySource", mySourceInstance);

// 注册鸭子类型来源（跨程序集）
HealthBarForecastRegistry.RegisterForeign("MyMod", "MySource", creature => 
{
    return GetForeignSegments(creature);
});

// 取消注册
HealthBarForecastRegistry.Unregister("MyMod", "MySource");
```

**鸭子类型来源要求**：
- 必须有公共实例属性 `Amount`（`int` 类型）
- 必须有公共实例属性 `Color`（`Godot.Color` 类型）
- 必须有公共实例属性 `Direction`（枚举或字符串，包含 "FromLeft" 或 "FromRight"）
- 可选属性：`Order`（`int`）、`OverlayMaterial`（`Material`）、`OverlaySelfModulate`（`Color?`）

## 自定义卡牌池 (CustomCardPoolModel)

继承 `CustomCardPoolModel` 来创建自定义卡牌池：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;

public class MyCustomCardPool : CustomCardPoolModel
{
    public MyCustomCardPool()
    {
        Name = "My Card Pool";
    }

    public override bool IsShared => false;

    public override Texture2D? CustomFrame(CustomCardModel card) => null;

    public override Color ShaderColor => new Color(1, 0, 0);

    public override float H => ShaderColor.H;
    public override float S => ShaderColor.S;
    public override float V => ShaderColor.V;

    protected override CardModel[] GenerateAllCards() => [];
}
```

**重要说明**：
- 所有卡牌池必须是角色池或共享池，否则无法被找到
- 角色池通过 `CharacterModel.CardPool` 属性获取
- 共享池通过 `ModelDb.AllSharedCardPools` 获取
- `IsShared` 为 true 时，池会自动注册到 `ModelDb.AllSharedCardPools`
- `CustomFrame` 用于自定义卡牌框贴图（Ancient 稀有度忽略此逻辑）
- 如果不重写 `CardFrameMaterialPath`，会自动使用 `ShaderColor` 生成 HSV 着色器材质
- 可通过 `BigEnergyIconPath` 和 `TextEnergyIconPath` 自定义能量图标

## 自定义药水 (CustomPotionModel)

继承 `CustomPotionModel` 来创建自定义药水：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

[Pool(typeof(SharedPotionPool))]
public class MyCustomPotion : CustomPotionModel
{
    public MyCustomPotion() : base(autoAdd: true)
    {
        Name = "My Potion";
        Description = "Heal 20 HP.";
    }

    public override string? CustomPackedImagePath => "res://textures/potions/my_potion.png";
    public override string? CustomPackedOutlinePath => "res://textures/potions/my_potion_outline.png";
}
```

**属性说明**：

| 属性 | 说明 |
|------|------|
| `CustomPackedImagePath` | 药水贴图路径（替代旧的 `PackedImagePath`） |
| `CustomPackedOutlinePath` | 药水轮廓贴图路径（替代旧的 `PackedOutlinePath`） |

**构造函数参数**：
- `autoAdd`：是否自动添加到内容字典（默认 true）

**注意**：旧版本的 `PackedImagePath` 和 `PackedOutlinePath` 属性已弃用，请使用 `CustomPackedImagePath` 和 `CustomPackedOutlinePath`。

## 自定义先古之民 (CustomAncientModel)

继承 `CustomAncientModel` 来创建自定义先古之民事件：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.Acts;

public class MyCustomAncient : CustomAncientModel
{
    public MyCustomAncient() : base(autoAdd: true)
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    protected override OptionPools MakeOptionPools => new(
        MakePool(ModelDb.Relic<SomeRelic>()),
        MakePool(ModelDb.Relic<AnotherRelic>())
    );

    public override string? CustomScenePath => "res://MyMod/scenes/ancients/my_ancient.tscn";
    public override string? CustomMapIconPath => "res://MyMod/images/ancients/my_ancient.png";
    public override string? CustomMapIconOutlinePath => "res://MyMod/images/ancients/my_ancient_outline.png";
    public override string? CustomRunHistoryIconPath => "res://MyMod/images/ui/run_history/my_ancient.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://MyMod/images/ui/run_history/my_ancient_outline.png";
}
```

**重要方法**：
- `IsValidForAct(ActModel act)`：检查先古之民是否适用于指定章节（推荐使用 `act.Id == ModelDb.Act<Hive>().Id` 检查）
- `ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)`：是否强制生成此先古之民（谨慎使用，可能导致模组冲突）
- `MakeOptionPools`：创建选项池（抽象属性，必须实现）
- `GenerateInitialOptions()`：生成初始选项列表（可选重写）

**视觉资源属性**：

| 属性 | 说明 |
|------|------|
| `CustomScenePath` | 自定义事件场景路径 |
| `CustomMapIconPath` | 地图图标路径 |
| `CustomMapIconOutlinePath` | 地图图标轮廓路径（新增） |
| `CustomRunHistoryIconPath` | 运行历史图标路径 |
| `CustomRunHistoryIconOutlinePath` | 运行历史图标轮廓路径（新增） |

**选项池工具**：
- `MakePool(params RelicModel[] options)`：从遗物模型创建加权列表
- `MakePool(params AncientOption[] options)`：从先古之民选项创建加权列表
- `AncientOption<T>(int weight, ...)`：创建先古之民选项（支持遗物预处理和变体）

**自定义选项示例**：

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new(this, ChooseCard, "MYMOD-MY_ANCIENT.pages.INITIAL.options.CHOOSE_CARD"),
        new(this, ChooseRelic, "MYMOD-MY_ANCIENT.pages.INITIAL.options.CHOOSE_RELIC"),
        new(this, UpgradeCards, "MYMOD-MY_ANCIENT.pages.INITIAL.options.UPGRADE_CARDS")
    };
}

private async Task ChooseCard()
{
    var cards = GetCustomCards();
    var cardReward = new CardReward(cardsToOffer, CardCreationSource.Other, Owner!);
    await RewardsCmd.OfferCustom(Owner!, [cardReward]);
    FinishEvent();
}

private void FinishEvent()
{
    var doneMethod = typeof(AncientEventModel).GetMethod("Done", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    doneMethod?.Invoke(this, null);
}
```

**本地化**：
先古之民事件使用特定的本地化键格式：
- 标题：`{ModId}-{AncientId}.title`
- 称号：`{ModId}-{AncientId}.epithet`
- 页面描述：`{ModId}-{AncientId}.pages.{PageName}.description`
- 选项：`{ModId}-{AncientId}.pages.{PageName}.options.{OptionId}.title` / `.description`
- 首次访问：`{ModId}-{AncientId}.talk.firstvisitEver.0-0.ancient`
- 角色对话：`{ModId}-{AncientId}.talk.{CharacterId}.{index}-{line}.ancient`
- 通用对话：`{ModId}-{AncientId}.talk.ANY.{index}-{line}.ancient`

**自定义场景**：
- 场景文件应放在 `res://MyMod/scenes/ancients/` 目录
- 需要配合 Harmony 补丁修改 `EventModel.BackgroundScenePath` 属性

### AncientOption 工具

`AncientOption` 是先古之民选项的抽象类：

```csharp
using BaseLib.Utils;

// 从遗物创建基础选项（隐式转换）
AncientOption option = ModelDb.Relic<MyRelic>();

// 创建带权重的选项
var weightedOption = new AncientOption<MyRelic>(weight: 2);

// 创建带预处理和变体的选项
var advancedOption = new AncientOption<MyRelic>(weight: 1)
{
    ModelPrep = relic => relic.Setup(),
    Variants = relic => new[] { relic, relic.UpgradedVersion }
};
```

**AncientOption 属性**：

| 属性 | 描述 |
|------|------|
| `Weight` | 选项权重，影响随机选择概率 |
| `AllVariants` | 所有变体遗物列表 |
| `ModelForOption` | 当前选项对应的遗物模型 |
| `ModelPrep` | 遗物预处理函数 |
| `Variants` | 变体生成函数 |

### OptionPools 构造

`OptionPools` 有三种构造方式：

```csharp
using BaseLib.Utils;

// 方式1：三个独立池（每个选项使用独立池）
var pools1 = new OptionPools(pool1, pool2, pool3);

// 方式2：两个池（前两个选项共用一个池）
var pools2 = new OptionPools(pool12, pool3);

// 方式3：单个池（所有选项共用一个池）
var pools3 = new OptionPools(pool);

// 获取所有选项
var allOptions = pools.AllOptions;

// 随机抽取选项
var selectedOptions = pools.Roll(rng);
```

## 自定义药水池 (CustomPotionPoolModel)

继承 `CustomPotionPoolModel` 来创建自定义药水池：

```csharp
using BaseLib.Abstracts;

public class MyCustomPotionPool : CustomPotionPoolModel
{
    public MyCustomPotionPool()
    {
        Name = "My Potion Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<PotionModel> GenerateAllPotions() => [];
}
```

**能量图标属性**：
- `BigEnergyIconPath`：大能量图标路径
- `TextEnergyIconPath`：文本能量图标路径

## 自定义能量球 (CustomOrbModel)

继承 `CustomOrbModel` 来创建自定义能量球：

```csharp
using BaseLib.Abstracts;
using Godot;

public class MyCustomOrb : CustomOrbModel
{
    public override string? CustomIconPath => "res://MyMod/images/orbs/my_orb_icon.png";
    public override string? CustomSpritePath => "res://MyMod/images/orbs/my_orb_sprite.png";

    public override bool IncludeInRandomPool => true;

    public override string? CustomPassiveSfx => "res://MyMod/audio/orbs/my_orb_passive.ogg";
    public override string? CustomEvokeSfx => "res://MyMod/audio/orbs/my_orb_evoke.ogg";
    public override string? CustomChannelSfx => "res://MyMod/audio/orbs/my_orb_channel.ogg";

    public override Node2D? CreateCustomSprite()
    {
        var sprite = new Sprite2D();
        sprite.Texture = GD.Load<Texture2D>(CustomSpritePath);
        return sprite;
    }
}
```

**属性说明**：

| 属性 | 说明 |
|------|------|
| `CustomIconPath` | 能量球图标路径（用于 UI 显示） |
| `CustomSpritePath` | 能量球精灵路径（用于战斗中显示） |
| `IncludeInRandomPool` | 是否包含在随机能量球池中（如 Chaos 卡牌） |
| `CustomPassiveSfx` | 被动音效路径 |
| `CustomEvokeSfx` | 激发音效路径 |
| `CustomChannelSfx` | 充能音效路径 |

**CreateCustomSprite 方法**：

重写此方法创建自定义精灵节点，如果返回 `null`，则使用 `CustomSpritePath` 或默认精灵。

**随机能量球池说明**：

当 `IncludeInRandomPool` 为 `true` 时，能量球会被添加到随机池中。随机池包含原版的 5 种能量球（Lightning、Frost、Dark、Plasma、Glass）加上所有自定义能量球，每个能量球有相同的被选中概率。

## 自定义遗物池 (CustomRelicPoolModel)

继承 `CustomRelicPoolModel` 来创建自定义遗物池：

```csharp
using BaseLib.Abstracts;

public class MyCustomRelicPool : CustomRelicPoolModel
{
    public MyCustomRelicPool()
    {
        Title = "My Relic Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
```

**能量图标属性**：
- `BigEnergyIconPath`：大能量图标路径
- `TextEnergyIconPath`：文本能量图标路径

## 自定义怪物 (CustomMonsterModel)

继承 `CustomMonsterModel` 来创建自定义怪物：

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

public class MyMonster : CustomMonsterModel
{
    public override string? CustomVisualPath => "res://MyMod/scenes/monsters/my_monster.tscn";
    
    public override int MinInitialHp => 50;
    public override int MaxInitialHp => 60;
    
    public override string? CustomAttackSfx => "res://MyMod/audio/monster_attack.ogg";
    public override string? CustomCastSfx => "res://MyMod/audio/monster_cast.ogg";
    public override string? CustomDeathSfx => "res://MyMod/audio/monster_death.ogg";
}
```

**视觉资源属性**：

| 属性 | 说明 |
|------|------|
| `CustomVisualPath` | 自定义视觉场景路径（默认查找 `res://scenes/creature_visuals/modname-class_name.tscn`） |
| `CustomAttackSfx` | 攻击音效路径 |
| `CustomCastSfx` | 施法音效路径 |
| `CustomDeathSfx` | 死亡音效路径 |

**动画设置**：

使用 `SetupAnimationState` 静态方法简化动画状态设置：

```csharp
public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
{
    return SetupAnimationState(
        controller,
        idleName: "idle",
        deadName: "dead",
        deadLoop: false,
        hitName: "hit",
        hitLoop: false,
        attackName: "attack",
        attackLoop: false,
        castName: "cast",
        castLoop: false
    );
}
```

**自定义视觉**：

重写 `CreateCustomVisuals` 方法创建自定义视觉节点：

```csharp
public override NCreatureVisuals? CreateCustomVisuals()
{
    if (CustomVisualPath == null) return null;
    return NodeFactory<NCreatureVisuals>.CreateFromScene(CustomVisualPath);
}
```

## 自定义遭遇 (CustomEncounterModel)

继承 `CustomEncounterModel` 来创建自定义遭遇（怪物组合）：

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

public class MyEncounter : CustomEncounterModel
{
    public MyEncounter() : base(RoomType.Monster, autoAdd: true)
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.ActNumber() == 1;

    public override string? CustomScenePath => "res://MyMod/scenes/encounters/my_encounter.tscn";

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<MyMonster1>(),
        ModelDb.Monster<MyMonster2>()
    ];

    public override IEnumerable<MutableMonster> GenerateMonsters()
    {
        if (Rng.NextFloat() < 0.5f)
        {
            return [ModelDb.Monster<MyMonster1>().ToMutable()];
        }
        return [ModelDb.Monster<MyMonster2>().ToMutable()];
    }
}
```

**构造函数参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `roomType` | RoomType | 房间类型（Monster、Elite、Boss） |
| `autoAdd` | bool | 是否自动添加到遭遇池（默认 true） |

**重要方法**：

| 方法 | 说明 |
|------|------|
| `IsValidForAct(ActModel)` | 检查遭遇是否适用于指定章节 |
| `AllPossibleMonsters` | 返回所有可能出现的怪物模型 |
| `GenerateMonsters()` | 生成实际出现的怪物实例 |

**视觉资源属性**：

| 属性 | 说明 |
|------|------|
| `CustomScenePath` | 自定义遭遇场景路径（1920x1080 Control，包含 Marker2D 标记敌人位置） |
| `HasScene` | 是否有场景（自动检测 CustomScenePath 或默认路径） |

**自定义背景**：

重写 `CustomEncounterBackground` 方法提供自定义背景：

```csharp
public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
{
    return new CustomBackgroundAssets(
        backgroundScenePath: "res://MyMod/scenes/backgrounds/my_bg.tscn",
        layerPaths: ["res://MyMod/images/backgrounds/layer1.png"]
    );
}
```

**场景结构要求**：

遭遇场景是一个 1920x1080 的 Control，包含 Marker2D 子节点标记敌人位置：
- 初始敌人会按场景中 Marker 的顺序放置
- 可以使用 CreatureCmd.Add 配合标记名称生成额外敌人

## 自定义宠物 (CustomPetModel)

继承 `CustomPetModel` 来创建自定义宠物（特殊的怪物类型）：

```csharp
using BaseLib.Abstracts;

public class MyPet : CustomPetModel
{
    public MyPet() : base(visibleHp: false)  // 是否显示血条
    {
    }

    public override string? CustomVisualPath => "res://MyMod/scenes/pets/my_pet.tscn";
    
    public override int MinInitialHp => 10;
    public override int MaxInitialHp => 10;
}
```

**CustomPetModel 特点**：

| 特性 | 说明 |
|------|------|
| 继承自 | `CustomMonsterModel` |
| 构造函数参数 | `visibleHp: bool` - 是否显示血条 |
| 默认行为 | 使用空的移动状态机（`NOTHING_MOVE`） |
| 视觉资源 | 与 `CustomMonsterModel` 相同 |

**自定义移动状态**：

宠物默认使用空的移动状态机。如果需要自定义行为，重写 `GenerateMoveStateMachine` 方法：

```csharp
protected override MonsterMoveStateMachine GenerateMoveStateMachine()
{
    MoveState idleState = new MoveState("IDLE", _ => Task.CompletedTask);
    idleState.FollowUpState = idleState;
    
    MoveState attackState = new MoveState("ATTACK", async _ => 
    {
        // 攻击逻辑
        await Task.CompletedTask;
    });
    attackState.FollowUpState = idleState;
    
    return new MonsterMoveStateMachine([idleState, attackState], idleState);
}
```

## 自定义 Modifier (ModifierModel)

继承 `ModifierModel` 来创建自定义修改器（如无尽模式）：

```csharp
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves.Runs;

public class MyModifier : ModifierModel
{
    public const string ModifierId = "MYMOD-MY_MODIFIER";

    [SavedProperty]
    public int MyMod_LoopCount { get; set; } = 0;

    [SavedProperty]
    public bool MyMod_HasStarted { get; set; } = false;

    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");

    public override string IconPath => "res://MyMod/images/modifiers/my_modifier.png";

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        if (MyMod_HasStarted)
        {
            return null;
        }
        return () => ActivateModifier(eventModel.Owner!, eventModel.Rng);
    }

    private async Task ActivateModifier(Player player, Rng rng)
    {
        MyMod_HasStarted = true;
        await CreatureCmd.GainMaxHp(player.Creature, 10m);
    }

    public override void AfterRunCreated(RunState runState)
    {
        MainFile.Logger.Info($"Modifier initialized");
    }

    public override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info($"Modifier loaded");
    }
}
```

**重要说明**：
- Modifier 需要通过 Harmony 补丁注册到 `ModelDb` 和 `GoodModifiers` 列表
- 使用 `[SavedProperty]` 属性标记需要持久化的属性
- BaseLib 会自动处理 SavedProperty 的注册，无需手动注入
- SavedProperty 检查器使用 `GetProperties` 方法，因此可以检测继承自基类的属性
- 属性命名建议使用前缀（如 `MyMod_`），否则会在日志中输出警告

**常用钩子方法**：
- `GenerateNeowOption(EventModel)`：生成 Neow 事件选项
- `AfterRunCreated(RunState)`：Run 创建后调用
- `AfterRunLoaded(RunState)`：Run 加载后调用
- `AfterRoomEntered(AbstractRoom)`：进入房间后调用
- `AfterCreatureAddedToCombat(Creature)`：生物加入战斗后调用
- `ShouldAllowAncient(Player, AncientEventModel)`：是否允许先古之民事件

**完整示例请参考 [自定义 Modifier](04-custom-modifier.md)**
