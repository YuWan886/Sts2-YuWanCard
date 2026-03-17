# 自定义动态变量

BaseLib 提供了三个自定义动态变量，用于实现特殊的卡牌效果。

## WithTooltip() 扩展方法

所有自定义变量都可以使用 `WithTooltip()` 方法添加提示框：

```csharp
using BaseLib.Extensions;

// 在变量构造时添加提示框
protected override IEnumerable<DynamicVar> CanonicalVars => [new PersistVar(2).WithTooltip()];
```

提示框本地化键格式：`{PREFIX}-{VAR_NAME}.title` 和 `{PREFIX}-{VAR_NAME}.description`。

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
