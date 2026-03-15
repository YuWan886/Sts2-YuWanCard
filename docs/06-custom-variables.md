# 自定义动态变量

BaseLib 提供了三个自定义动态变量，用于实现特殊的卡牌效果。

## PersistVar

表示卡牌的"持续"次数（每回合打出次数限制）：

```csharp
using BaseLib.Cards.Variables;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用
protected override IEnumerable<DynamicVar> CanonicalVars => [new PersistVar(2)];

// 获取剩余次数
int remaining = PersistVar.PersistCount(card, 2);
```

**用途**：用于实现"本回合可打出 X 次"的卡牌效果。每回合开始时重置计数。

## RefundVar

表示卡牌打出后的能量返还：

```csharp
using BaseLib.Cards.Variables;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用
protected override IEnumerable<DynamicVar> CanonicalVars => [new RefundVar(1)];
```

**用途**：用于实现"打出后返还 X 点能量"的卡牌效果。

## ExhaustiveVar

表示卡牌的"耗尽"次数（整场游戏中打出次数限制，至少保留 1 次）：

```csharp
using BaseLib.Cards.Variables;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用
protected override IEnumerable<DynamicVar> CanonicalVars => [new ExhaustiveVar(3)];

// 获取剩余次数
int remaining = ExhaustiveVar.ExhaustiveCount(card, 3);
```

**用途**：用于实现"本场战斗总共可打出 X 次"的卡牌效果。整场战斗有效，且至少保留 1 次机会。

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

public class MyCustomVar : DynamicVar
{
    public const string Key = "MyCustom";

    public MyCustomVar(decimal value) : base(Key, value)
    {
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
