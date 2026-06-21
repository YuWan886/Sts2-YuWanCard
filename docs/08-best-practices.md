# 最佳实践

## 代码规范

### 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 公共成员 | PascalCase | `public int MaxHealth` |
| 私有成员 | camelCase | `private int currentCount` |
| 常量 | PascalCase 或全大写 | `public const int MaxValue = 100` |
| 事件处理器 | `On` 前缀 | `OnTurnStart` |

### 注释规范

- 使用 XML 文档注释（`///`）为公共 API 添加说明
- 保持代码简洁，避免不必要的注释
- 注释应简洁明了，避免复杂的逻辑描述
- 代码应自解释，注释仅用于说明"为什么"而非"做什么"

### 文件组织

- 每个类一个文件，文件名与类名一致
- 使用 `#region` 组织大型类
- 成员顺序：常量 → 字段 → 属性 → 构造函数 → 方法 → 事件

---

## 日志记录

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

---

## 本地化

### 本地化文件结构

```
YuWanCard/localization/
├── zhs/                  # 简体中文
│   ├── cards.json        # 卡牌
│   ├── powers.json       # 能力
│   ├── relics.json       # 遗物
│   ├── ancients.json     # 先古之民对话/标题
│   ├── modifiers.json    # 修改器
│   ├── events.json       # 事件
│   ├── monsters.json     # 怪物
│   ├── enchantments.json # 附魔
│   ├── orbs.json         # 充能球
│   ├── potions.json      # 药水
│   ├── characters.json   # 角色
│   ├── badges.json       # 徽章
│   ├── encounters.json   # 遭遇
│   ├── rest_site_ui.json # 休息站选项
│   ├── card_reward_ui.json # 卡牌奖励 UI
│   ├── gameplay_ui.json  # 游戏 UI
│   └── settings_ui.json  # 设置界面
└── eng/                  # 英文
    └── ... (同上)
```

### 本地化键格式

| 类型 | 键格式 | 说明 |
|------|--------|------|
| 卡牌标题 | `YUWANCARD-{CardId}.title` | 动态变量在 description 中可用 |
| 卡牌描述 | `YUWANCARD-{CardId}.description` | 支持动态变量 |
| 卡牌选牌提示 | `YUWANCARD-{CardId}.selectionScreenPrompt` | 需要选牌时提供 |
| 能力标题 | `YUWANCARD-{PowerId}.title` | — |
| 能力描述 | `YUWANCARD-{PowerId}.description` | 静态文本，图鉴显示 |
| 能力智能描述 | `YUWANCARD-{PowerId}.smartDescription` | 支持动态变量，战斗悬浮提示 |
| 能力远程描述 | `YUWANCARD-{PowerId}.remoteDescription` | 多人游戏中其他玩家施加时 |
| 能力选牌提示 | `YUWANCARD-{PowerId}.selectionScreenPrompt` | 需要选牌/选目标时 |
| 遗物标题 | `YUWANCARD-{RelicId}.title` | — |
| 遗物描述 | `YUWANCARD-{RelicId}.description` | — |
| 遗物风味 | `YUWANCARD-{RelicId}.flavor` | 非功能性风味文本 |
| 修改器标题 | `YUWANCARD-{ModifierId}.title` | — |
| 修改器描述 | `YUWANCARD-{ModifierId}.description` | — |
| 修改器 Neow | `YUWANCARD-{ModifierId}.neow_title/neow_description` | Neow 选项 |

### Android 本地化前缀回退

`LocalizationPrefixFallbackPatch` 在 `LocTable.GetRawText` 抛出 `LocException` 时自动重试添加 `YUWANCARD-` 前缀的键查找，解决 Android 平台上前缀丢失的问题。

### 描述文本最佳实践

**卡牌** description 支持动态变量（自动注入）：
```json
{
  "YUWANCARD-PIG_STRIKE.description": "造成 {Damage:diff()} 点伤害。",
  "YUWANCARD-PIG_SLEEP.description": "结束你的回合\n获得 {Block:diff()} 点 [gold]格挡[/gold]\n恢复 {Heal:diff()} 点生命"
}
```

**能力** description 只能写静态文本，动态变量写在 smartDescription：
```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪疑惑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "每回合获得1个随机的[gold]能力[/gold]。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "每回合获得{PigDoubtPower}个随机的[gold]能力[/gold]。"
}
```

---

## 卡牌设计

### 基类选择

| 基类 | 适用场景 |
|------|----------|
| `YuWanCardModel` | 推荐使用，自动 ID 和路径生成，流式构建器 API |

### 设计原则

1. **费用平衡**：费用应反映卡牌效果强度。1 费 ≈ 6 伤害 ≈ 5 格挡
2. **稀有度控制**：Common 简单直接，Uncommon 有机制交互，Rare 有独特效果
3. **升级合理**：升级通常增加约 30-50% 效果（+3 伤害、+2 格挡、-1 费用等）
4. **目标类型正确**：Self 用于增益，AnyEnemy 用于单体攻击，AllEnemies 用于 AOE
5. **标签一致**：攻击牌使用 CardTag.Strike，让打击系协同生效
6. **升级不手动管理关键字**：使用 `WithKeyword(keyword, UpgradeType.Add/Remove)` 声明升级行为，`ConstructedUpgrade()` 自动处理，不要在 `OnUpgrade()` 中手动 AddKeyword/RemoveKeyword

### 持久化卡牌状态

对于需要在跨战斗间记住状态的卡牌（如"永久升级"类卡牌），使用 `[SavedProperty]` + `BaseReplayCount` + `DeckVersion` 模式：

```csharp
public class Sha : YuWanCardModel
{
    [SavedProperty] public int YUWANCARD_PermanentReplayCount { get; set; }

    static Sha() { SavedPropertyRegistration.RegisterType(typeof(Sha)); }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        BaseReplayCount = YUWANCARD_PermanentReplayCount;
    }

    protected override async Task OnPlay(...)
    {
        // ... 执行效果 ...
        
        // 同步状态回牌组
        if (DeckVersion is Sha deckSha)
        {
            deckSha.YUWANCARD_PermanentReplayCount += 1;
            deckSha.BaseReplayCount = deckSha.YUWANCARD_PermanentReplayCount;
        }
    }
}
```

**关键点**：
- `BaseReplayCount`：允许卡牌一回合多次打出
- `DeckVersion`：指向牌组中的规范卡牌实例，修改它才能跨战斗持久化
- 必须调用 `SavedPropertyRegistration.RegisterType(typeof(MyCard))` 注册类型

### 常见卡牌模式

**单体攻击**：
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    if (cardPlay.Target != null)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
    }
}
```

**AOE 攻击**：
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    foreach (var enemy in choiceContext.CombatState.Enemies)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(enemy).Execute(choiceContext);
    }
}
```

**获得格挡**：
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
}
```

**施加能力**：
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    var powerAmount = DynamicVars.GetPowerVar<StrengthPower>()!.BaseValue;
    await PowerCmd.Apply<StrengthPower>(Owner.Creature, powerAmount, Owner.Creature, this);
}
```

**对目标施加能力**：
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    if (cardPlay.Target != null)
    {
        var powerAmount = DynamicVars.GetPowerVar<VulnerablePower>()!.BaseValue;
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, powerAmount, Owner.Creature, this);
    }
}
```

**多段伤害**（使用 WithHitCount）：
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    if (cardPlay.Target != null)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .Execute(choiceContext);
    }
}
```

### 卡牌实现模板

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(SharedCardPool))]
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

---

## 能力设计

### 能力安全性检查

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

---

## 遗物设计

### 存档属性

使用 `[SavedProperty]` 标记需要持久化的属性：

```csharp
[SavedProperty]
public int YuWanCard_EndlessLoopCount { get; set; } = 0;

[SavedProperty]
public bool YuWanCard_HasStarted { get; set; } = false;
```

**重要**：属性命名建议使用模组前缀（如 `YuWanCard_`），否则会产生警告。

### 金币修改保护

使用 `GoldModificationGuard` 避免递归调用，搭配新版 API：

```csharp
private GoldModificationGuard? _goldGuard;

private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
    () => Owner,
    amount => Math.Floor(amount * 0.5m),
    async amount => await PlayerCmd.LoseGold(amount, Owner!)
);

public override decimal ModifyGoldGained(Player player, decimal amount)
{
    return GoldGuard.ModifyGoldGained(player, amount);
}

public override async Task AfterModifyingGoldGained(Player player, decimal amount)
{
    await GoldGuard.AfterModifyingGoldGained(player, amount);
}
```

---

## Harmony 补丁

### 补丁类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| `[HarmonyPrefix]` | 方法执行前 | 阻止原方法、修改参数 |
| `[HarmonyPostfix]` | 方法执行后 | 修改返回值、添加副作用 |
| `[HarmonyTranspiler]` | IL 代码修改 | 深度修改方法逻辑 |
| `[HarmonyFinalizer]` | 异常处理 | 捕获异常并执行回退逻辑 |

### 补丁实现模板

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

### 反射调用私有方法

```csharp
// 使用 YuWanReflectionHelper
var value = YuWanReflectionHelper.GetPrivateField<SomeType>(instance, "_fieldName");
YuWanReflectionHelper.SetPrivateField(instance, "_fieldName", newValue);

// 使用 AccessTools
var fieldRef = AccessTools.FieldRefAccess<SomeType, string>("_someField");
```

---

## 性能优化

### 缓存结果

对于频繁调用的方法，缓存结果：

```csharp
private static readonly ConcurrentDictionary<Type, bool> SafetyCache = new();

public static bool IsSafePower(PowerModel power)
{
    var powerType = power.GetType();
    if (SafetyCache.TryGetValue(powerType, out var isSafe))
    {
        return isSafe;
    }
    
    bool result = AnalyzePowerSafety(powerType);
    SafetyCache[powerType] = result;
    return result;
}
```

### 避免频繁的字符串操作

使用 `StringBuilder` 或字符串插值：

```csharp
// 推荐
var message = $"Processing card: {card.Id}";

// 避免
var message = "Processing card: " + card.Id;
```

---

## 错误处理

### 异常捕获

```csharp
try
{
    await RiskyOperation();
}
catch (Exception ex)
{
    MainFile.Logger.Error($"Operation failed: {ex.Message}");
    // 可选：恢复或回滚
}
```

### 空值检查

```csharp
// 使用模式匹配
if (cardPlay.Target is Creature target)
{
    await Attack(target);
}

// 使用 null 条件运算符
await cardPlay.Target?.ApplyPower(power);
```

---

## 多人游戏

### 多人游戏限制

```csharp
// 仅限多人模式的卡牌
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.MultiplayerOnly;

// 仅限单人模式的卡牌
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.SingleplayerOnly;
```

### 玩家身份检查

```csharp
if (LocalContext.IsMe(player))
{
    // 只对本地玩家执行
    await CreatureCmd.GainMaxHp(player.Creature, 10m);
}
```
