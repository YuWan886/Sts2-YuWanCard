# 扩展功能

## 模组互操作 (ModInterop)

BaseLib 提供了模组间互操作系统，允许模组之间进行软依赖交互：

### 创建互操作类

```csharp
using BaseLib.Utils.ModInterop;

[ModInterop("OtherModId")]
public class OtherModInterop : InteropClassWrapper
{
    [InteropTarget]
    public static MethodInfo? SomeMethod { get; set; }

    [InteropTarget]
    public static Type? SomeType { get; set; }

    public static void DoSomething()
    {
        if (SomeMethod != null)
        {
            SomeMethod.Invoke(null, new object[] { "arg" });
        }
    }
}
```

**特性说明**：

| 特性 | 用途 |
|------|------|
| `[ModInterop("ModId")]` | 标记互操作类，指定目标模组 ID |
| `[InteropTarget]` | 标记互操作目标（方法、类型等） |

### 使用互操作

```csharp
// 检查目标模组是否加载
if (OtherModInterop.IsLoaded)
{
    OtherModInterop.DoSomething();
}

// 获取目标模组的类型
var targetType = OtherModInterop.SomeType;
if (targetType != null)
{
    var instance = Activator.CreateInstance(targetType);
}
```

**重要说明**：
- ModInterop 使用软依赖，目标模组不存在时不会报错
- 互操作类会在目标模组加载时自动绑定
- 使用前检查 `IsLoaded` 或目标是否为 null

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

## IL 补丁工具

BaseLib 提供了简化 Transpiler 编写的工具：

### InstructionPatcher 示例

```csharp
using BaseLib.Utils.Patching;
using HarmonyLib;

[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public class MyTranspilerPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patcher = new InstructionPatcher(instructions);

        // 查找并替换方法调用
        while (patcher.Find(new IMatcher[]
        {
            InstructionMatcher.OpCode(OpCodes.Call, AccessTools.Method(typeof(TargetClass), "OldMethod"))
        }))
        {
            patcher.GetLabels(out var labels);
            patcher.Replace(new CodeInstruction(OpCodes.Call, 
                AccessTools.Method(typeof(MyClass), "NewMethod")).WithLabels(labels));
        }

        return patcher;
    }
}
```

### 复杂匹配示例

```csharp
// 匹配多个指令序列
while (patcher.Find(new IMatcher[]
{
    InstructionMatcher.OpCode(OpCodes.Ldarg_0),
    InstructionMatcher.OpCode(OpCodes.Ldfld, someField),
    InstructionMatcher.OpCode(OpCodes.Callvirt, someMethod)
}))
{
    // 在匹配位置前插入代码
    patcher.Step(-1);
    patcher.Insert(new[]
    {
        new CodeInstruction(OpCodes.Ldarg_0),
        new CodeInstruction(OpCodes.Call, myCheckMethod)
    });
}
```

### InstructionMatcher 流式 API

```csharp
var matcher = new InstructionMatcher(instructions);

// 流式匹配
if (matcher
    .Match(OpCodes.Ldarg_0)
    .Match(OpCodes.Call, methodA)
    .Match(OpCodes.Stloc_0)
    .Success)
{
    // 匹配成功，处理代码
}
```

## 自定义牌堆 (CustomPile)

继承 `CustomPile` 创建自定义牌堆：

```csharp
using BaseLib.Abstracts;

public class MyCustomPile : CustomPile
{
    public MyCustomPile(Player player) : base(player)
    {
    }

    public override string PileName => "My Custom Pile";

    // 自定义牌堆逻辑
}
```

**使用 SpireField 存储自定义牌堆**：

```csharp
private static readonly SpireField<PlayerCombatState, MyCustomPile> MyPileField = new(() => null!);

public static MyCustomPile GetMyPile(Player player)
{
    var pile = MyPileField.Get(player.PlayerCombatState);
    if (pile == null)
    {
        pile = new MyCustomPile(player);
        MyPileField.Set(player.PlayerCombatState, pile);
    }
    return pile;
}
```

## IHealAmountModifier 接口

实现 `IHealAmountModifier` 接口可以修改治疗量：

```csharp
using BaseLib.Abstracts;

public class MyHealModifier : IHealAmountModifier
{
    public decimal ModifyHealAdditive(Creature creature, decimal amount)
    {
        return amount + 5; // 额外治疗 5 点
    }

    public decimal ModifyHealMultiplicative(Creature creature, decimal amount)
    {
        return amount * 1.5m; // 治疗 150%
    }
}
```

**执行顺序**：
1. `IHealAmountModifier.ModifyHealAdditive()`
2. `AbstractModel.ModifyHealAmount()`
3. `IHealAmountModifier.ModifyHealMultiplicative()`

## 自定义能量图标池

实现 `ICustomEnergyIconPool` 接口为卡牌池添加自定义能量图标：

```csharp
using BaseLib.Abstracts;

public class MyCardPool : CustomCardPoolModel, ICustomEnergyIconPool
{
    public string? BigEnergyIconPath => "res://MyMod/images/ui/energy_big.png";
    public string? TextEnergyIconPath => "res://MyMod/images/ui/energy_text.png";
}
```
