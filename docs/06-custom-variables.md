# 自定义动态变量

BaseLib 提供了多种自定义动态变量，用于实现特殊的卡牌效果。

## DynamicVar 扩展方法

BaseLib 提供了 `DynamicVarExtensions` 类，包含以下扩展方法：

```csharp
// 为动态变量添加提示框
var myVar = new MyCustomVar(5m).WithTooltip();

// 为动态变量设置升级值
var damageVar = new DamageVar(6m).WithUpgrade(3m);  // 升级后 9 点伤害

// 计算格挡值（考虑各种加成）
decimal block = blockVar.CalculateBlock(creature, ValueProp.None, cardPlay, card);
```

### WithTooltip()

`WithTooltip()` 方法会自动从 `static_hover_tips` 本地化表中读取提示文本，键名格式为 `{PREFIX}-{VAR_NAME}.title` 和 `{PREFIX}-{VAR_NAME}.description`。

### WithUpgrade(decimal upgradeValue)

`WithUpgrade()` 方法为动态变量设置升级增加值：

```csharp
// 创建伤害变量，基础值 6，升级增加 3
var damageVar = new DamageVar(6m).WithUpgrade(3m);

// 等价于：
var damageVar = new DamageVar(6m);
damageVar.UpgradeValue = 3m;
```

### CalculateBlock()

计算实际格挡值，考虑各种加成：

```csharp
decimal actualBlock = blockVar.CalculateBlock(
    creature,        // 目标生物
    ValueProp.None,  // 值属性
    cardPlay,        // 卡牌打出上下文
    card             // 卡牌模型
);
```

## PersistVarDynamicVar 基类

`DynamicVar` 是所有动态变量的基类，定义在 `MegaCrit.Sts2.Core.Localization.DynamicVars` 命名空间。

### 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 变量名称，用于在本地化字符串中引用 |
| `BaseValue` | decimal | 基础数值 |
| `EnchantedValue` | decimal | 附魔后的数值 |
| `PreviewValue` | decimal | 预览显示的数值（考虑所有修正后） |
| `IntValue` | int | BaseValue 的整数形式 |
| `WasJustUpgraded` | bool | 标记是否刚刚升级过（用于高亮显示） |

### 核心方法

| 方法 | 说明 |
|------|------|
| `ResetToBase()` | 重置为基础值 |
| `UpgradeValueBy(decimal addend)` | 升级时增加数值 |
| `UpdateCardPreview(CardModel, CardPreviewMode, Creature?, bool)` | 更新卡牌预览显示（子类重写） |
| `Clone()` | 克隆变量 |

## DynamicVarSet 集合类

`DynamicVarSet` 管理一组 DynamicVar，通过 `CardModel.DynamicVars` 访问。

### 预定义便捷属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Block` | BlockVar | 格挡变量 |
| `Damage` | DamageVar | 伤害变量 |
| `Energy` | EnergyVar | 能量变量 |
| `Heal` | HealVar | 治疗变量 |
| `this[string key]` | DynamicVar | 通过键访问变量 |

## 变量对比

| 变量 | 作用范围 | 重置时机 | 用途 |
|------|----------|----------|------|
| `PersistVar` | 本回合 | 每回合开始 | "本回合可打出 X 次" |
| `ExhaustiveVar` | 本场战斗 | 不重置 | "本场战斗总共可打出 X 次"（至少保留 1 次） |
| `RefundVar` | 即时 | 无 | "打出后返还 X 点能量" |

## WithTooltip() 扩展方法

所有自定义变量都可以使用 `WithTooltip()` 方法添加提示框：

```csharp
using BaseLib.Extensions;

// 在变量构造时添加提示框（自动生成本地化键）
protected override IEnumerable<DynamicVar> CanonicalVars => [new PersistVar(2).WithTooltip()];

// 自定义本地化键
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new PersistVar(2).WithTooltip("CUSTOM_KEY", "my_table")];
```

**本地化键格式**：
- 自动生成：`{PREFIX}-{VAR_NAME}.title` 和 `{PREFIX}-{VAR_NAME}.description`
- 自定义：`{locKey}.title` 和 `{locKey}.description`

**本地化文件示例**（`static_hover_tips.json`）：

```json
{
  "MYMOD-PERSIST.title": "持续",
  "MYMOD-PERSIST.description": "此牌本回合打出后返回手牌，可重复 {Persist} 次",
  "MYMOD-EXHAUSTIVE.title": "究极",
  "MYMOD-EXHAUSTIVE.description": "此牌可打出 {Exhaustive} 次后消耗",
  "MYMOD-REFUND.title": "退还",
  "MYMOD-REFUND.description": "打出后退还 {Refund} 点能量"
}
```

## PersistVar

表示卡牌的"持续"次数（每回合打出次数限制）：

```csharp
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用（带提示框）
protected override IEnumerable<DynamicVar> CanonicalVars => [new PersistVar(2).WithTooltip()];

// 获取剩余次数
int remaining = PersistVar.PersistCount(card, 2);
```

**用途**：用于实现"本回合可打出 X 次"的卡牌效果。每回合开始时重置计数。

**本地化键**：`{PREFIX}-PERSIST.title` 和 `{PREFIX}-PERSIST.description`

## RefundVar

表示卡牌打出后的能量返还：

```csharp
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用（带提示框）
protected override IEnumerable<DynamicVar> CanonicalVars => [new RefundVar(1).WithTooltip()];
```

**用途**：用于实现"打出后返还 X 点能量"的卡牌效果。

**本地化键**：`{PREFIX}-REFUND.title` 和 `{PREFIX}-REFUND.description`

## ExhaustiveVar

表示卡牌的"耗尽"次数（整场游戏中打出次数限制，至少保留 1 次）：

```csharp
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用（带提示框）
protected override IEnumerable<DynamicVar> CanonicalVars => [new ExhaustiveVar(3).WithTooltip()];

// 获取剩余次数
int remaining = ExhaustiveVar.ExhaustiveCount(card, 3);
```

**用途**：用于实现"本场战斗总共可打出 X 次"的卡牌效果。整场战斗有效，且至少保留 1 次机会。

**本地化键**：`{PREFIX}-EXHAUSTIVE.title` 和 `{PREFIX}-EXHAUSTIVE.description`

**与 PersistVar 的区别**：
- `PersistVar`：每回合重置，用于"本回合可打出 X 次"的卡牌
- `ExhaustiveVar`：整场游戏有效，用于"本场战斗总共可打出 X 次"的卡牌，且至少保留 1 次机会

## 创建自定义动态变量

你可以创建自己的动态变量：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Extensions;

public class MyCustomVar : DynamicVar
{
    public const string Key = "MyCustom";

    public MyCustomVar(decimal value) : base(Key, value)
    {
        this.WithTooltip(); // 自动添加提示框
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        PreviewValue = IntValue * 2;
    }
}
```

**使用自定义变量**：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => [new MyCustomVar(5)];
```

**本地化描述**：

```json
{
  "MYMOD-MYCARD.description": "造成 {MyCustom:diff()} 点伤害。"
}
```

**提示框本地化**（`static_hover_tips.json`）：

```json
{
  "MYMOD-MY_CUSTOM.title": "自定义变量",
  "MYMOD-MY_CUSTOM.description": "这是一个自定义变量的说明。"
}
```

## DynamicVarExtensions 工具类

BaseLib 提供了 `DynamicVarExtensions` 扩展类：

### WithTooltip()

为动态变量添加提示框：

```csharp
var myVar = new MyCustomVar(5m).WithTooltip();
```

### CalculateBlock()

计算格挡值（考虑各种加成）：

```csharp
using BaseLib.Extensions;

decimal block = blockVar.CalculateBlock(creature, ValueProp.None, cardPlay, card);
```

**参数说明**：
- `creature`：获得格挡的生物
- `props`：值属性标志
- `cardPlay`：卡牌打出上下文（可选）
- `cardSource`：源卡牌（可选）
