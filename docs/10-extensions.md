# 扩展功能

## 自定义卡牌变量

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

## 自定义遗物升级

遗物可以设置升级替换：

```csharp
public override RelicModel? GetUpgradeReplacement()
{
    return new MyUpgradedRelic();
}
```

**完整示例**：

```csharp
[Pool(typeof(SharedRelicPool))]
public class MyRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override RelicModel? GetUpgradeReplacement() => new MyUpgradedRelic();
}

[Pool(typeof(SharedRelicPool))]
public class MyUpgradedRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
}
```

## 自定义先古之民选项

创建带有变体的先古之民选项：

```csharp
protected override OptionPools MakeOptionPools => new OptionPools(
    MakePool(
        AncientOption<MyRelic>(
            weight: 1,
            relicPrep: relic => relic.Setup(),
            makeAllVariants: relic => new[] { relic, relic.UpgradedVersion }
        )
    )
);
```

**选项参数说明**：
- `weight`：选项权重，影响随机选择概率
- `relicPrep`：遗物预处理函数，用于在生成前配置遗物
- `makeAllVariants`：生成所有变体的函数，用于创建多个版本的遗物

## 自定义卡牌池

创建自定义卡牌池：

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

    protected override CardModel[] GenerateAllCards() => 
    [
        ModelDb.Card<Card1>(),
        ModelDb.Card<Card2>(),
        ModelDb.Card<Card3>()
    ];
}
```

**重要说明**：
- 所有卡牌池必须是角色池或共享池，否则无法被找到
- 角色池通过 `CharacterModel.CardPool` 属性获取
- 共享池通过 `ModelDb.AllSharedCardPools` 获取
- `IsShared` 为 true 时，池会自动注册到 `ModelDb.AllSharedCardPools`

## 自定义遗物池

创建自定义遗物池：

```csharp
using BaseLib.Abstracts;

public class MyCustomRelicPool : CustomRelicPoolModel
{
    public MyCustomRelicPool()
    {
        Name = "My Relic Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<RelicModel> GenerateAllRelics() => 
    [
        ModelDb.Relic<Relic1>(),
        ModelDb.Relic<Relic2>()
    ];
}
```

## 自定义药水池

创建自定义药水池：

```csharp
using BaseLib.Abstracts;

public class MyCustomPotionPool : CustomPotionPoolModel
{
    public MyCustomPotionPool()
    {
        Name = "My Potion Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<PotionModel> GenerateAllPotions() => 
    [
        ModelDb.Potion<Potion1>(),
        ModelDb.Potion<Potion2>()
    ];
}
```

## Harmony 补丁技巧

### 修改私有方法

使用反射调用私有方法：

```csharp
using System.Reflection;
using HarmonyLib;

var method = typeof(TargetClass).GetMethod("PrivateMethod", 
    BindingFlags.NonPublic | BindingFlags.Instance);
method?.Invoke(instance, new object[] { arg1, arg2 });
```

### 修改属性

使用 `AccessTools` 获取和设置属性：

```csharp
using HarmonyLib;

var property = AccessTools.Property(typeof(TargetClass), "PropertyName");
var value = property.GetValue(instance);
property.SetValue(instance, newValue);
```

### 修改字段

使用 `AccessTools` 获取和设置字段：

```csharp
using HarmonyLib;

var field = AccessTools.Field(typeof(TargetClass), "fieldName");
var value = field.GetValue(instance);
field.SetValue(instance, newValue);
```

### 创建自定义字段

使用 `SpireField` 创建自定义字段：

```csharp
using BaseLib.Utils;

private static readonly SpireField<Creature, int> MyCustomField = new(() => 0);

MyCustomField.Set(creature, 10);
var value = MyCustomField.Get(creature);

MyCustomField[creature] = 20;
```
