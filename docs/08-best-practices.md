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
├── zhs/               # 简体中文
│   ├── cards.json
│   ├── powers.json
│   ├── relics.json
│   ├── ancients.json
│   ├── modifiers.json
│   └── events.json
└── eng/               # 英文
    ├── cards.json
    ├── powers.json
    └── ...
```

### 本地化键格式

| 类型 | 键格式 |
|------|--------|
| 卡牌标题 | `YUWANCARD-{CardId}.title` |
| 卡牌描述 | `YUWANCARD-{CardId}.description` |
| 能力标题 | `YUWANCARD-{PowerId}.title` |
| 能力描述 | `YUWANCARD-{PowerId}.description` |
| 遗物标题 | `YUWANCARD-{RelicId}.title` |
| 遗物描述 | `YUWANCARD-{RelicId}.description` |

### Android 本地化前缀回退

`LocalizationPrefixFallbackPatch` 在 `LocTable.GetRawText` 抛出 `LocException` 时自动重试添加 `YUWANCARD-` 前缀的键查找，解决 Android 平台上前缀丢失的问题。

### 描述文本最佳实践

```json
{
  "YUWANCARD-PIG_STRIKE.description": "造成 {Damage:diff()} 点伤害。",
  "YUWANCARD-PIG_DOUBT.description": "每回合获得 {PigDoubtPower:diff()} 个随机的 [gold]能力[/gold]。",
  "YUWANCARD-PIG_SLEEP.description": "结束你的回合\n获得 {Block:diff()} 点 [gold]格挡[/gold]\n恢复 {Heal:diff()} 点生命"
}
```

---

## 卡牌设计

### 基类选择

| 基类 | 适用场景 |
|------|----------|
| `YuWanCardModel` | 推荐使用，自动 ID 和路径生成，流式构建器 API |

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

## 平台检测

使用 `RuntimePlatform` 替代直接调用 `OS.HasFeature("mobile")`：

```csharp
// 检测移动平台
if (RuntimePlatform.IsMobileLike)
{
    // Android/iOS 特殊处理
}

// 检测是否支持动态代码生成（emit/transpiler）
if (RuntimePlatform.SupportsDynamicCode)
{
    // Reflection.Emit 安全使用
}
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
