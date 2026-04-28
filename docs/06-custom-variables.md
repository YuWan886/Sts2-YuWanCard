# 自定义变量

## 概述

游戏使用 `DynamicVar` 系统来处理卡牌和能力的动态数值。这些变量支持升级、本地化格式和描述文本中的占位符。

## DynamicVar 基类

所有动态变量都继承自 `DynamicVar` 基类：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public class MyCustomVar : DynamicVar
{
    public MyCustomVar(decimal baseValue) : base("MyCustomVar", baseValue) { }
    
    public override string FormatValue(decimal value, string? format = null)
    {
        return $"自定义格式: {value}";
    }
}
```

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

重复次数变量：

```csharp
// 在卡牌构造函数中
WithRepeat(3);

// 升级时
DynamicVars.Repeat.UpgradeValueBy(1m);
```

## 特殊变量类型

### PersistVar（持续次数）

每回合可打出 X 次的卡牌：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;

protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new PersistVar(2m)];  // 每回合可打出 2 次
```

**特点**：
- 每回合开始时重置次数
- 打出卡牌时减少次数
- 次数用完后无法打出

### RefundVar（能量返还）

打出后返还 X 点能量：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new RefundVar(1m)];  // 打出后返还 1 点能量
```

**特点**：
- 打出卡牌后自动返还能量
- 常用于 0 费卡牌的平衡

### ExhaustiveVar（耗尽次数）

本场战斗总共可打出 X 次，至少保留 1 次：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new ExhaustiveVar(3m)];  // 本场战斗总共可打出 3 次
```

**特点**：
- 每场战斗开始时重置
- 打出时减少次数
- 至少保留 1 次（不会完全耗尽）

## CalculatedDamageVar / CalculatedBlockVar

计算伤害/格挡，支持基础值、倍率和加成：

```csharp
// 伤害 = (基础值 + 加成) * 倍率
var damage = new CalculatedDamageVar(
    baseValue: 6m,    // 基础伤害
    multiplier: 2m,   // 倍率
    bonus: 3m         // 加成
);
// 结果: (6 + 3) * 2 = 18

var block = new CalculatedBlockVar(
    baseValue: 5m,
    multiplier: 1m,
    bonus: 2m
);
```

## CanonicalVars

在能力或卡牌中定义规范变量，用于描述文本中的占位符：

```csharp
// 能力示例
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new DynamicVar("PigDoubtPower", 1m)];

// 多个变量
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

创建自定义变量类型：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public class MyCustomVar : DynamicVar
{
    public MyCustomVar(decimal baseValue) : base("MyCustomVar", baseValue) { }
    
    public override string FormatValue(decimal value, string? format = null)
    {
        return format switch
        {
            "percent" => $"{value * 100}%",
            "time" => $"{value}次",
            _ => value.ToString()
        };
    }
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
  "YUWANCARD-MY_CARD.description": "触发{MyCustomVar:time}效果。"
}
```

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
