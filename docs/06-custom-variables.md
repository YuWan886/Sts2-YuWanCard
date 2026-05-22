# 自定义变量

## 概述

游戏使用 `DynamicVar` 系统来处理卡牌和能力的动态数值。这些变量支持升级、本地化格式和描述文本中的占位符。

**重要区分**：
- **卡牌** `description`：自动调用 `DynamicVars.AddTo()` → 可以使用所有 DynamicVar 占位符
- **能力** `description`：不会自动注入 DynamicVar → 只能写静态文本
- **能力** `smartDescription`：当能力实例化在生物身上时，自动调用 `DynamicVars.AddTo()` → 可以使用 DynamicVar 占位符

## DynamicVar 基类

所有动态变量都继承自 `DynamicVar` 基类：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public class MyCustomVar : DynamicVar
{
    public MyCustomVar(decimal baseValue) : base("MyCustomVar", baseValue) { }
}
```

DynamicVar 通过 `IConvertible` 接口参与 SmartFormat 格式化，内置格式化器（`diff()`、`energyIcons()`、`D` 等）由引擎提供。

## 内置变量类型

### DamageVar

伤害变量：

```csharp
// 在卡牌构造函数中
WithDamage(6);

// 升级时
DynamicVars.Damage.UpgradeValueBy(3m);

// 获取基础值
decimal baseDamage = DynamicVars.Damage.BaseValue;
```

### BlockVar

格挡变量：

```csharp
// 在卡牌构造函数中
WithBlock(5);

// 升级时
DynamicVars.Block.UpgradeValueBy(2m);
```

### HealVar

治疗变量：

```csharp
// 在卡牌构造函数中
WithHeal(10);

// 升级时
DynamicVars.Heal.UpgradeValueBy(3m);
```

### EnergyVar

能量变量：

```csharp
// 在卡牌构造函数中
WithEnergy(1);

// 升级时
DynamicVars.Energy.UpgradeValueBy(1m);
```

### PowerVar

能力层数变量：

```csharp
// 在卡牌构造函数中
WithPower<StrengthPower>(2);

// 升级时
DynamicVars.GetPowerVar<StrengthPower>()?.UpgradeValueBy(1m);
```

### CardsVar

卡牌数量变量：

```csharp
// 在卡牌构造函数中
WithCards(3);

// 升级时
DynamicVars.Cards.UpgradeValueBy(1m);
```

### RepeatVar

重复次数变量（通过 `DynamicVars.Repeat` 访问，用于 `DamageCmd.Attack().WithHitCount()` 等）：

## CalculatedDamageVar

计算伤害，支持基础值、倍率和加成：

```csharp
// 在卡牌构造函数中使用 WithCalculatedDamage
WithCalculatedDamage(
    ValueProp.Move,                               // 伤害属性
    (card, target) => card.CombatState?.Enemies?.Count ?? 0, // 倍率函数
    baseVal: 6,                                   // 基础伤害
    extraVal: 0,                                  // 额外伤害（乘倍率前）
    baseUpgrade: 3,                               // 基础伤害升级值
    extraUpgrade: 0                               // 额外伤害升级值
);
// 计算伤害 = (CalculationBase + CalculationExtra × 倍率) × 全局伤害系数
// 自动创建三个 DynamicVar：CalculationBaseVar、ExtraDamageVar、CalculatedDamageVar
// 本地化: {CalculatedDamage:diff()} 显示最终值
```

### CalculationBaseVar

计算基数变量：

```csharp
var baseVar = new CalculationBaseVar(6m);
```

### ExtraDamageVar

额外伤害变量：

```csharp
var extraDamage = new ExtraDamageVar(3m);
```

## CanonicalVars

在能力或卡牌中定义规范变量，用于描述文本中的占位符。

**使用位置差异**：
- **卡牌**：`CanonicalVars` 中的变量会自动注入到 `description` → 可以直接使用占位符
- **能力**：`CanonicalVars` 中的变量仅在 `smartDescription`（和 `remoteDescription`）中可用，`description` 不会注入

```csharp
// 能力示例（变量只在 smartDescription 中可用）
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new DynamicVar("PigDoubtPower", 1m)];

// 卡牌示例（变量在 description 中可用）
protected override IEnumerable<DynamicVar> CanonicalVars => 
[
    new DynamicVar("Damage", 6m),
    new DynamicVar("Block", 5m),
    new DynamicVar("Heal", 3m)
];
```

## 变量格式化

### 基础格式化

```csharp
// 默认格式
DynamicVars.Damage.ToString();  // "6"

// 自定义格式
DynamicVars.Damage.ToString("F1");  // "6.0"
```

### diff() 方法

在描述文本中使用 `diff()` 显示升级后的差异：

```json
{
  "YUWANCARD-PIG_STRIKE.description": "造成{Damage:diff()}点伤害。"
}
```

**显示效果**：
- 未升级：造成 6 点伤害。
- 升级后：造成 9 点伤害。（数字会高亮显示变化）

## 自定义 DynamicVar

创建自定义变量类型，继承 `DynamicVar`：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public class MyCustomVar : DynamicVar
{
    public MyCustomVar(decimal baseValue) : base("MyCustomVar", baseValue) { }
}
```

**使用自定义变量**：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new MyCustomVar(5m)];
```

**本地化**：

```json
{
  "YUWANCARD-MY_CARD.description": "触发{MyCustomVar}效果。"
}
```

**注意**：DynamicVar 的格式化通过 SmartFormat 的 `IConvertible` 接口实现。内置格式化器（`diff()`、`energyIcons()`、`D`、`F1`、`P0` 等）由游戏引擎的 SmartFormat 扩展提供。自定义格式化器可通过 SmartFormat 的 `IFormatter` 接口注册。

## DynamicVars 属性访问

卡牌的 `DynamicVars` 属性提供对所有变量的访问：

```csharp
// 标准变量
var damage = DynamicVars.Damage;
var block = DynamicVars.Block;
var heal = DynamicVars.Heal;
var energy = DynamicVars.Energy;
var cards = DynamicVars.Cards;
var repeat = DynamicVars.Repeat;

// 能力变量
var strengthVar = DynamicVars.GetPowerVar<StrengthPower>();

// 自定义变量
var customVar = DynamicVars.GetVar<MyCustomVar>();
```

## 变量升级

### UpgradeValueBy

增加指定数值：

```csharp
DynamicVars.Damage.UpgradeValueBy(3m);  // +3 伤害
```

### UpgradeValueTo

升级到指定数值：

```csharp
DynamicVars.Damage.UpgradeValueTo(9m);  // 升级到 9
```

### 检查是否已升级

```csharp
if (DynamicVars.Damage.IsUpgraded)
{
    // 已升级
}
```

## 变量类型汇总

### 数值变量

| 类型 | 说明 | 默认名称 | 示例 |
|------|------|---------|------|
| `DamageVar` | 伤害变量 | `Damage` | `new DamageVar(6m)` |
| `BlockVar` | 格挡变量 | `Block` | `new BlockVar(5m, ValueProp.None)` |
| `HealVar` | 治疗变量 | `Heal` | `new HealVar(10m)` |
| `EnergyVar` | 能量变量 | `Energy` | `new EnergyVar(1m)` |
| `PowerVar<T>` | 能力层数变量 | `类名` | `new PowerVar<StrengthPower>(2m)` |
| `CardsVar` | 卡牌数量变量 | `Cards` | `new CardsVar(3m)` |
| `RepeatVar` | 重复次数变量 | `Repeat` | `new RepeatVar(3m)` |
| `ForgeVar` | 锻造值变量 | `Forge` | `new ForgeVar(3)` |
| `GoldVar` | 金币数量变量 | `Gold` | `new GoldVar(50)` |
| `MaxHpVar` | 最大生命值变量 | `MaxHp` | `new MaxHpVar(10m)` |
| `HpLossVar` | 生命损失变量 | `HpLoss` | `new HpLossVar(3m)` |
| `StarsVar` | 星星数量变量 | `Stars` | `new StarsVar(3)` |
| `SummonVar` | 召唤数量变量 | `Summon` | `new SummonVar(1m)` |
| `IntVar` | 通用整数变量 | 自定义 | `new IntVar("MyInt", 5m)` |
| `DynamicVar` | 通用命名变量 | 自定义 | `new DynamicVar("MyVar", 1m)` |

### 计算变量

| 类型 | 说明 | 依赖 |
|------|------|------|
| `CalculatedDamageVar` | 计算伤害 `(基础 + 额外) × 倍率` | `CalculationBase` + `CalculationExtra` + multiplier |
| `CalculatedBlockVar` | 计算格挡 `(基础 + 额外) × 倍率` | `CalculationBase` + `CalculationExtra` + multiplier |
| `CalculatedVar` | 通用计算变量（需 `WithMultiplier`） | `CalculationBase` + `CalculationExtra` + multiplier |
| `CalculationBaseVar` | 计算基数值 | — |
| `CalculationExtraVar` / `ExtraDamageVar` | 计算额外值（每层倍率加成的基数） | — |
| `OstyDamageVar` | Osty 攻击伤害 | — |

### 特殊变量

| 类型 | 说明 | 示例 |
|------|------|------|
| `BoolVar` | 布尔变量 | `new BoolVar("IsActive", true)` |
| `StringVar` | 字符串变量 | `new StringVar("StatusName", "燃烧")` |
| `IfUpgradedVar` | 升级条件变量 | 由卡牌系统自动注入，无需手动添加 |

### 布尔变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new BoolVar("HasBuff", true)];

// 本地化中使用
// "description": "{HasBuff:hasBuff|有增益|无增益}"
```

### 字符串变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new StringVar("StatusName", "燃烧")];

// 本地化中直接使用
// "description": "施加{StatusName}效果。"
```
