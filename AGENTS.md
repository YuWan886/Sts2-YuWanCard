---
alwaysApply: true
description: AI 开发指南 - YuWanCard 模组开发规范与最佳实践
---
# AI 开发指南

本文档为 AI 助手提供 YuWanCard 模组开发的详细规范、最佳实践和参考资源。

---

## 开发规范

### 代码规范

1. **命名约定**
   - 公共成员：PascalCase（如 `public int MaxHealth`）
   - 私有成员：camelCase（如 `private int currentCount`）
   - 常量：PascalCase 或全大写（如 `public const int MaxValue = 100`）
   - 事件处理器：`On` 前缀（如 `OnTurnStart`）

2. **注释规范**
   - 使用 XML 文档注释（`///`）为公共 API 添加说明
   - 保持代码简洁，避免不必要的注释
   - 注释应简洁明了，避免复杂的逻辑描述
   - 代码应自解释，注释仅用于说明"为什么"而非"做什么"

3. **文件组织**
   - 每个类一个文件，文件名与类名一致
   - 使用 `#region` 组织大型类
   - 成员顺序：常量 → 字段 → 属性 → 构造函数 → 方法 → 事件

### 命名空间规范

```
YuWanCard                    # 根命名空间
YuWanCard.Cards              # 卡牌
YuWanCard.Powers             # 能力
YuWanCard.Relics             # 遗物
YuWanCard.Characters         # 角色
YuWanCard.Monsters           # 怪物
YuWanCard.Events             # 事件
YuWanCard.Modifiers          # 修改器
YuWanCard.Enchantments       # 附魔
YuWanCard.Ancients           # 先古之民
YuWanCard.Patches            # Harmony 补丁
YuWanCard.Utils              # 工具类
YuWanCard.Config             # 配置
YuWanCard.Commands           # 调试命令
```

### ID 命名规范

| 类型 | 格式 | 示例 |
|------|------|------|
| 卡牌 | `YUWANCARD-{snake_case_id}` | `YUWANCARD-PIG_STRIKE` |
| 能力 | `YUWANCARD-{snake_case_id}` | `YUWANCARD-PIG_DOUBT_POWER` |
| 遗物 | `YUWANCARD-{snake_case_id}` | `YUWANCARD-RING_OF_SEVEN_CURSES` |
| 本地化键 | `{ModId}-{Id}.{property}` | `YUWANCARD-PIG_STRIKE.title` |

---

## BaseLib 框架

BaseLib 是一个为 Slay the Spire 2 (StS2) 模组开发提供基础功能的库，它简化了模组开发过程，提供了各种抽象类和工具来帮助开发者创建自定义内容。

### 抽象基类

| 基类 | 用途 |
|------|------|
| `CustomCardModel` | 自定义卡牌基类 |
| `ConstructedCardModel` | 链式 API 卡牌基类（推荐） |
| `CustomCharacterModel` | 自定义角色基类 |
| `CustomRelicModel` | 自定义遗物基类 |
| `CustomPowerModel` | 自定义能力基类 |
| `CustomPotionModel` | 自定义药水基类 |
| `CustomAncientModel` | 自定义先古之民基类 |
| `CustomCardPoolModel` | 自定义卡牌池基类 |
| `CustomRelicPoolModel` | 自定义遗物池基类 |
| `CustomPotionPoolModel` | 自定义药水池基类 |
| `CustomSingletonModel` | 持续接收钩子的单例模型基类 |

### 接口

| 接口 | 用途 |
|------|------|
| `ICustomModel` | 标记接口，用于确定是否需要添加模组前缀到 ID |
| `ICustomPower` | 自定义能力接口，可与其他能力类一起继承 |
| `IHealAmountModifier` | 治疗量修改器接口 |

### 工具类

| 类 | 用途 |
|------|------|
| `PoolAttribute` | 内容池属性标记，用于将自定义内容注册到正确的池 |
| `CommonActions` | 常用游戏动作工具（攻击、格挡、抽牌、施加能力等） |
| `ModelDb` | 游戏模型数据库，用于获取和注册各种游戏模型 |
| `GodotUtils` | Godot 节点和场景处理工具 |
| `ShaderUtils` | 着色器生成工具 |
| `WeightedList` | 加权随机列表 |
| `SpireField` | Harmony 自定义字段（基于 ConditionalWeakTable） |
| `AncientDialogueUtil` | 先古之民对话本地化工具 |
| `OptionPools` | 先古之民选项池构建工具 |
| `AncientOption` | 先古之民选项抽象类 |
| `GameVersionCompat` | 游戏版本兼容性工具，用于处理不同游戏版本的 API 差异 |

### 自动注册机制

继承自 `ICustomModel` 的类在构造时会自动注册到对应的内容池：

- `CustomCardModel`：自动添加到 `PoolAttribute` 指定的卡牌池
- `CustomRelicModel`：自动添加到 `PoolAttribute` 指定的遗物池
- `CustomPotionModel`：自动添加到 `PoolAttribute` 指定的药水池
- `CustomAncientModel`：自动添加到先古之民列表
- `CustomCardPoolModel`：如果 `IsShared` 为 true，自动注册到共享卡牌池列表

### ID 前缀系统

BaseLib 会自动为所有实现 `ICustomModel` 接口的模型添加模组前缀，确保不同模组的内容不会冲突。前缀基于类型的命名空间生成。

### 格挡自动检测

`CustomCardModel` 的 `GainsBlock` 属性会自动检测 `DynamicVars` 中是否包含 `BlockVar` 或 `CalculatedBlockVar`，无需手动设置。

### 自定义图标路径

所有自定义模型都支持通过属性指定自定义图标路径：

```csharp
// 卡牌
public override string? CustomPortraitPath => "res://MyMod/images/card_portraits/my_card.png";
public override Texture2D? CustomFrame => GD.Load<Texture2D>("res://MyMod/images/card_frames/my_frame.png");

// 能力
public override string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png"; // 64x64
public override string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";    // 256x256

// 遗物
public override string? PackedImagePath => "res://MyMod/images/relics/my_relic.png";
public override string? PackedOutlinePath => "res://MyMod/images/relics/my_relic_outline.png";

// 药水
public override string? PackedImagePath => "res://MyMod/images/potions/my_potion.png";
public override string? PackedOutlinePath => "res://MyMod/images/potions/my_potion_outline.png";

// 先古之民
public override string? CustomScenePath => "res://MyMod/scenes/ancients/my_ancient.tscn";
public override string? CustomMapIconPath => "res://MyMod/images/ancients/my_ancient.png";
public override Texture2D? CustomRunHistoryIcon => GD.Load<Texture2D>("res://MyMod/images/ui/run_history/my_ancient.png");
```

### DynamicVar 扩展方法

BaseLib 提供了 `DynamicVarExtensions` 类，包含以下扩展方法：

```csharp
// 为动态变量添加提示框
var myVar = new MyCustomVar(5m).WithTooltip();

// 计算格挡值（考虑各种加成）
decimal block = blockVar.CalculateBlock(creature, ValueProp.None, cardPlay, card);
```

`WithTooltip()` 方法会自动从 `static_hover_tips` 本地化表中读取提示文本，键名格式为 `{PREFIX}-{VAR_NAME}.title` 和 `{PREFIX}-{VAR_NAME}.description`。

---

## 最佳实践

### 日志记录

使用 `MainFile.Logger` 进行日志记录：

```csharp
// Info：重要操作（初始化、保存、加载）
MainFile.Logger.Info("Endless mode activated!");

// Debug：详细调试信息（进度计算、卡牌过滤）
MainFile.Logger.Debug($"Processing card: {card.Id}");

// Warn：警告信息（卡牌未找到、配置缺失）
MainFile.Logger.Warn($"Card not found: {cardId}");

// Error：错误信息（异常捕获）
MainFile.Logger.Error($"Failed to apply power: {ex.Message}");
```

**日志位置**：`%AppData%\SlayTheSpire2\logs\godot.log`

### 本地化

本地化文件位于 `YuWanCard/localization/{lang}/` 目录：

| 文件 | 内容 | 键格式 |
|------|------|--------|
| cards.json | 卡牌 | `YUWANCARD-{CardId}.title` / `.description` / `.selectionScreenPrompt` |
| powers.json | 能力 | `YUWANCARD-{PowerId}.title` / `.description` / `.smartDescription` |
| relics.json | 遗物 | `YUWANCARD-{RelicId}.title` / `.description` / `.flavor` / `.additionalRestSiteHealText` |
| ancients.json | 先古之民 | `YUWANCARD-{AncientId}.title` / `.epithet` |
| modifiers.json | 修改器 | `YUWANCARD-{ModifierId}.title` / `.description` |
| events.json | 事件 | `YUWANCARD-{EventId}.pages.{PageName}.description` |

**描述文本支持 BBCode 和占位符**：
```json
{
  "YUWANCARD-PIG_STRIKE.description": "造成{Damage:diff()}点伤害。",
  "YUWANCARD-PIG_DOUBT.description": "每回合获得{PigDoubtPower:diff()}个随机的[gold]能力[/gold]。",
  "YUWANCARD-PIG_SLEEP.description": "结束你的回合\n获得{Block:diff()}点[gold]格挡[/gold]\n恢复{Heal:diff()}点生命"
}
```

### 卡牌设计

#### 基类选择

| 基类 | 适用场景 |
|------|----------|
| `ConstructedCardModel` | 推荐使用，链式 API 简洁 |
| `CustomCardModel` | 传统方式，需要更多控制时使用 |
| `YuWanCardModel` | 项目基类，自动 ID 和路径生成 |

#### 卡牌实现模板

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class MyCard : YuWanCardModel
{
    public MyCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6);
        WithTags(CardTag.Strike);
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

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

#### TargetType 目标类型

| TargetType | 说明 |
|------------|------|
| `Self` | 自身 |
| `AllAllies` | 所有队友（包括自己） |
| `AnyAlly` | 任意队友（包括自己） |
| `AllEnemies` | 所有敌人 |
| `AnyEnemy` | 任意敌人 |
| `RandomEnemy` | 随机敌人 |
| `AnyPlayer` | 任意玩家（可用于选择死亡玩家） |
| `None` | 无目标 |

#### 多人游戏限制

```csharp
// 仅限多人模式的卡牌
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.MultiplayerOnly;
```

#### 能力提示显示

```csharp
// 方式1：构造函数中使用 WithTip
public MyCard() : base(...)
{
    WithPower<StrengthPower>(2);
    WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<StrengthPower>()));
}

// 方式2：重写 ExtraHoverTips 属性（仅 CustomCardModel）
public override IEnumerable<TooltipSource> ExtraHoverTips
{
    get
    {
        foreach (var tip in base.ExtraHoverTips)
            yield return tip;
        yield return new TooltipSource(_ => HoverTipFactory.FromPower<MyPower>());
    }
}
```

### 能力设计

#### 能力实现模板

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class MyPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new DynamicVar("MyPower", 1m)];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
        }
    }
}
```

#### PowerType 类型

| 类型 | 说明 | 颜色 |
|------|------|------|
| `Buff` | 增益效果 | 绿色 |
| `Debuff` | 减益效果 | 红色 |
| `Neutral` | 中性效果 | 蓝色 |

#### PowerStackType 类型

| 类型 | 说明 |
|------|------|
| `Counter` | 层数叠加 |
| `Duration` | 持续时间 |
| `None` | 不叠加 |

#### 能力安全性检查

赋予玩家随机能力时必须检查安全性：

```csharp
private bool IsSafePower(PowerModel power)
{
    // 排除模组自定义能力
    if (power is YuWanPowerModel)
        return false;
    
    // 使用 IL 分析检查安全性
    return PowerSafetyUtils.IsSafePower(power);
}
```

**不安全的能力特征**：
- 包含怪物专属逻辑
- 调用 `MonsterModel` 类型转换
- 未正确处理 `dealer` 参数的空值检查

### 遗物设计

#### 遗物实现模板

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class MyRelic : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    [SavedProperty]
    private bool SomeState { get; set; }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        // 获得时的效果
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + 1 : amount;
    }
}
```

#### 遗物稀有度

| 稀有度 | 说明 | 掉落率 |
|--------|------|--------|
| `Common` | 普通 | 50% |
| `Uncommon` | 罕见 | 35% |
| `Rare` | 稀有 | 15% |
| `Ancient` | 先古之民 | 特殊获取 |
| `Shop` | 商店 | 仅商店购买 |

#### 常用钩子方法

| 方法 | 说明 | 示例用途 |
|------|------|----------|
| `AfterObtained()` | 获得遗物时触发 | 初始化效果 |
| `AfterPlayerTurnStart()` | 玩家回合开始时 | 每回合效果 |
| `AfterCombatVictory()` | 战斗胜利后 | 战后结算 |
| `ModifyDamageMultiplicative()` | 修改伤害倍率 | 伤害加成/减免 |
| `ModifyBlockMultiplicative()` | 修改格挡倍率 | 格挡加成/减免 |
| `ModifyMaxEnergy()` | 修改最大能量 | 能量加成 |
| `ModifyHandDraw()` | 修改抽牌数 | 抽牌加成 |
| `ModifyRestSiteHealAmount()` | 修改休息处回复 | 回复调整 |
| `TryModifyRewards()` | 修改奖励 | 额外奖励 |
| `ShouldGainGold()` | 获得金币前 | 金币修改 |
| `AfterGoldGained()` | 获得金币后 | 金币结算 |

#### 存档属性

使用 `[SavedProperty]` 标记需要持久化的属性：

```csharp
[SavedProperty]
public int YuWanCard_EndlessLoopCount { get; set; } = 0;

[SavedProperty]
public bool YuWanCard_HasStarted { get; set; } = false;
```

**重要**：属性命名建议使用模组前缀（如 `YuWanCard_`），否则会产生警告。

### Harmony 补丁

#### 补丁实现模板

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Events;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Neow))]
class MyNeowPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialOptions")]
    static void AddCustomOption(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        var options = __result.ToList();
        options.Add(new EventOption(
            __instance,
            async () => {
                // 选项效果
                await RelicCmd.Obtain<MyRelic>(__instance.Owner);
            },
            new LocString("events", "MY_OPTION.title"),
            new LocString("events", "MY_OPTION.description")
        ));
        __result = options;
    }
}
```

#### 补丁类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| `[HarmonyPrefix]` | 方法执行前 | 阻止原方法、修改参数 |
| `[HarmonyPostfix]` | 方法执行后 | 修改返回值、添加副作用 |
| `[HarmonyTranspiler]` | IL 代码修改 | 深度修改方法逻辑 |

#### 反射调用私有方法

```csharp
var doneMethod = typeof(AncientEventModel).GetMethod("Done", 
    BindingFlags.NonPublic | BindingFlags.Instance);
doneMethod?.Invoke(ancientEvent, null);
```

### 版本兼容性

游戏有 main 和 beta 两个分支，API 存在差异。使用 `GameVersionCompat` 处理：

```csharp
// 检测当前分支
if (GameVersionCompat.IsBetaBranch)
{
    // beta 分支特有代码
}
else if (GameVersionCompat.IsMainBranch)
{
    // main 分支特有代码
}

// 使用统一 API
GameVersionCompat.TalkCmdPlay(line, speaker, VfxColor.Red, -1.0);

// 检测 API 可用性
if (GameVersionCompat.HasModifyEnergyGainHook)
{
    // 使用能量获取钩子
}
```

**当前版本常量**：
- main 分支：0.99.1
- beta 分支：0.102.0

---

## 配置系统

### ModConfig 基类

```csharp
using BaseLib.Config;

public class MyModConfig : SimpleModConfig
{
    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool ShowDeathOverlay { get; set; } = true;

    [ConfigSection("游戏设置")]
    [ConfigHoverTip]
    public static int MaxCards { get; set; } = 10;

    public MyModConfig() : base() { }
}
```

### 配置注册

```csharp
// 在 MainFile.Initialize() 中注册
Config = new YuWanCardConfig();
ModConfigRegistry.Register(ModId, Config);
Config.ConfigChanged += OnConfigChanged;
```

### SavedProperty 属性

用于持久化保存属性（与配置不同，这些会保存到存档中）：

```csharp
[SavedProperty]
public int YuWanCard_EndlessLoopCount { get; set; } = 0;

[SavedProperty]
public bool YuWanCard_HasStarted { get; set; } = false;
```

---

## 自定义动态变量

### PersistVar（持续次数）

每回合可打出 X 次的卡牌：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new PersistVar(2m)];  // 每回合可打出 2 次
```

### RefundVar（能量返还）

打出后返还 X 点能量：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new RefundVar(1m)];  // 打出后返还 1 点能量
```

### ExhaustiveVar（耗尽次数）

本场战斗总共可打出 X 次，至少保留 1 次：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new ExhaustiveVar(3m)];  // 本场战斗总共可打出 3 次
```

---

## 参考资源

### 项目内资源

| 资源 | 路径 | 说明 |
|------|------|------|
| 开发文档 | `docs/` | 分主题的开发指南 |
| 游戏源代码（beta） | `others/sts2-beta-src/` | beta 分支游戏源码 |
| 游戏源代码（main） | `others/sts2-main-src/` | main 分支游戏源码 |
| BaseLib 项目 | `others/BaseLib-StS2-master/` | BaseLib 源码和示例 |

### 文档目录

| 文档 | 说明 |
|------|------|
| `docs/01-project-setup.md` | 项目设置、引用 BaseLib、项目结构 |
| `docs/02-core-features.md` | 卡牌、角色、遗物、能力、药水、先古之民等核心功能 |
| `docs/03-config-system.md` | 模组配置和 SavedProperty 属性 |
| `docs/04-custom-modifier.md` | 创建自定义游戏模式修改器 |
| `docs/05-utils.md` | GodotUtils、CommonActions、ModelDb 等工具 |
| `docs/06-custom-variables.md` | PersistVar、RefundVar、ExhaustiveVar |
| `docs/07-bbcode-and-placeholders.md` | BBCode 标签、占位变量、Formatter 格式化器 |
| `docs/08-best-practices.md` | 命名约定、调试、性能优化 |
| `docs/09-examples.md` | 完整的模组示例代码 |
| `docs/10-troubleshooting.md` | 常见问题和解决方案 |
| `docs/11-extensions.md` | 自定义变量、遗物升级等扩展功能 |
| `docs/12-version-compatibility.md` | 游戏版本兼容性工具和统一 API 接口 |

### DynamicVar 类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `DamageVar` | 伤害变量 | `new DamageVar(6m)` |
| `BlockVar` | 格挡变量 | `new BlockVar(5m, ValueProp.None)` |
| `HealVar` | 治疗变量 | `new HealVar(10m)` |
| `EnergyVar` | 能量变量 | `new EnergyVar(1m)` |
| `PowerVar<T>` | 能力层数变量 | `new PowerVar<StrengthPower>(2m)` |
| `CardsVar` | 卡牌数量变量 | `new CardsVar(3m)` |
| `RepeatVar` | 重复次数变量 | `new RepeatVar(3m)` |
| `CalculatedDamageVar` | 计算伤害 | `new CalculatedDamageVar(6m, 2m, 3m)` |
| `CalculatedBlockVar` | 计算格挡 | `new CalculatedBlockVar(5m, 1m, 2m)` |
| `PersistVar` | 持续次数 | `new PersistVar(2m)` |
| `RefundVar` | 能量返还 | `new RefundVar(1m)` |
| `ExhaustiveVar` | 耗尽次数 | `new ExhaustiveVar(3m)` |

### MCP 工具

| 工具 | 用途 |
|------|------|
| `context7` | 文档查询（查询 BaseLib 等库文档） |
| `github` | GitHub 代码搜索 |

### 智能体

| 智能体 | 用途 |
|--------|------|
| `Search` | 代码库搜索 |

### 外部链接

- [BaseLib 项目](https://github.com/Alchyr/BaseLib-StS2)

---

## 常见问题

### Q: 如何添加新的卡牌池？

继承 `CustomCardPoolModel`：

```csharp
public class MyCardPool : CustomCardPoolModel
{
    public override string Title => "my_pool";
    public override bool IsShared => false;
    public override bool IsColorless => false;
    
    protected override CardModel[] GenerateAllCards() => 
    [
        ModelDb.Card<MyCard1>(),
        ModelDb.Card<MyCard2>()
    ];
}
```

### Q: 如何让卡牌仅在多人模式出现？

```csharp
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.MultiplayerOnly;
```

### Q: 如何正确处理金币修改？

使用 `GoldModificationGuard` 避免递归调用：

```csharp
private GoldModificationGuard? _goldGuard;

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
```

### Q: 如何检测游戏版本？

```csharp
var version = GameVersionCompat.GameVersion;
if (GameVersionCompat.IsBetaBranch) { /* beta */ }
if (GameVersionCompat.IsMainBranch) { /* main */ }
```

### Q: 如何添加先古之民对话？

本地化键格式：
- 首次访问：`{ModId}-{AncientId}.talk.firstvisitEver.0-0.ancient`
- 角色对话：`{ModId}-{AncientId}.talk.{CharacterId}.{index}-{line}.ancient`
- 通用对话：`{ModId}-{AncientId}.talk.ANY.{index}-{line}.ancient`

### Q: 如何使用 CommonActions？

```csharp
// 卡牌攻击
var attackCmd = CommonActions.CardAttack(this, cardPlay, hitCount: 1);
await choiceContext.RunCommand(attackCmd);

// 卡牌格挡
var blockCmd = CommonActions.CardBlock(this, cardPlay);
await choiceContext.RunCommand(blockCmd);
```

### Q: 如何创建自定义 DynamicVar？

继承 `DynamicVar` 类：

```csharp
public class MyCustomVar : DynamicVar
{
    public MyCustomVar(decimal baseValue) : base("MyCustomVar", baseValue) { }
    
    public override string FormatValue(decimal value, string? format = null)
    {
        return $"自定义格式: {value}";
    }
}
```
