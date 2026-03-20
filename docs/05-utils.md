# 工具类

## CommonActions

提供一些常见的游戏动作：

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;

// 攻击命令（从卡牌和 CardPlay 获取目标）
var attackCmd = CommonActions.CardAttack(card, cardPlay, hitCount: 2);

// 攻击命令（指定目标）
var attackCmd = CommonActions.CardAttack(card, target, damage: 10, hitCount: 1);

// 攻击命令（带特效）
var attackCmd = CommonActions.CardAttack(card, cardPlay, hitCount: 1, vfx: "path/to/vfx", sfx: "path/to/sfx");

// 格挡
var blockAmount = await CommonActions.CardBlock(card, cardPlay);

// 抽牌
var drawnCards = await CommonActions.Draw(card, context);

// 给目标施加能力
var power = await CommonActions.Apply<StrengthPower>(target, card, 5);

// 给自己施加能力
var power = await CommonActions.ApplySelf<StrengthPower>(card, 5);

// 选择多张卡牌
var selectedCards = await CommonActions.SelectCards(card, selectionPrompt, context, PileType.Hand, 2);

// 选择卡牌（指定范围）
var selectedCards = await CommonActions.SelectCards(card, selectionPrompt, context, PileType.Hand, minCount: 1, maxCount: 3);

// 选择单张卡牌
var selectedCard = await CommonActions.SelectSingleCard(card, selectionPrompt, context, PileType.Hand);
```

### CardAttack 攻击命令

| 重载方法 | 说明 |
|----------|------|
| `CardAttack(CardModel, CardPlay, int hitCount, string? vfx, string? sfx, string? tmpSfx)` | 从 CardPlay 获取目标 |
| `CardAttack(CardModel, Creature?, int hitCount, string? vfx, string? sfx, string? tmpSfx)` | 指定目标，自动获取伤害值 |
| `CardAttack(CardModel, Creature?, decimal damage, int hitCount, string? vfx, string? sfx, string? tmpSfx)` | 指定目标和伤害值 |

**支持的目标类型**：
- `TargetType.AnyEnemy`：单个敌人
- `TargetType.AllEnemies`：所有敌人
- `TargetType.RandomEnemy`：随机敌人

**不支持的目标类型**：`TargetType.Self`、`TargetType.AllAllies` 等非攻击目标类型

### 其他方法

| 方法 | 说明 |
|------|------|
| `CardBlock(CardModel, CardPlay)` | 获得格挡 |
| `CardBlock(CardModel, BlockVar, CardPlay)` | 使用自定义格挡值 |
| `Draw(CardModel, PlayerChoiceContext)` | 抽取 DynamicVars.Cards 指定数量的牌 |
| `Apply<T>(Creature, CardModel?, decimal, bool silent)` | 给目标应用能力 |
| `ApplySelf<T>(CardModel, decimal, bool silent)` | 给自己应用能力 |
| `SelectCards(CardModel, LocString, PlayerChoiceContext, PileType, int)` | 选择指定数量的卡牌 |
| `SelectCards(CardModel, LocString, PlayerChoiceContext, PileType, int minCount, int maxCount)` | 选择范围内的卡牌 |
| `SelectSingleCard(CardModel, LocString, PlayerChoiceContext, PileType)` | 选择单张卡牌 |

## 扩展方法

BaseLib 提供了多个扩展方法类，简化常见操作：

### ActModelExtensions

```csharp
using BaseLib.Extensions;

// 获取章节编号（1/2/3，-1 表示未知）
int actNumber = actModel.ActNumber();
```

### DynamicVarExtensions

```csharp
using BaseLib.Extensions;

// 为变量添加提示框（自动生成本地化键 {PREFIX}-{VAR_NAME}）
var myVar = new PersistVar(2).WithTooltip();

// 为变量添加提示框（自定义本地化键）
var myVar = new PersistVar(2).WithTooltip("CUSTOM_KEY", "my_table");

// 计算格挡值（考虑各种加成）
decimal block = blockVar.CalculateBlock(creature, ValueProp.None, cardPlay, card);
```

### StringExtensions

```csharp
using BaseLib.Extensions;

// 移除 ID 前缀（格式：PREFIX-ORIGINALID → ORIGINALID）
string id = "MYMOD-MY_CARD".RemovePrefix(); // 返回 "MY_CARD"
```

### TypePrefix

```csharp
using BaseLib.Extensions;

// 获取类型的前缀（基于命名空间，格式：NAMESPACE-）
string prefix = typeof(MyCard).GetPrefix();

// 获取根命名空间
string rootNs = typeof(MyCard).GetRootNamespace();
```

### IEnumerableExtensions

```csharp
using BaseLib.Extensions;

// 格式化为可读字符串
var items = new[] { "a", "b", "c" };
string readable = items.AsReadable(); // "a,b,c"
string readableWithSeparator = items.AsReadable(" | "); // "a | b | c"

// 带行号的输出
string numbered = items.NumberedLines();
// 输出:
// 0: a
// 1: b
// 2: c
```

### FloatExtensions

```csharp
using BaseLib.Extensions;

// 根据快速模式调整时间
float delay = 0.5f.OrFast();
// 普通模式: 0.5f
// 快速模式: 0.15f
// 瞬间模式: 0.01f
```

### HarmonyExtensions

```csharp
using BaseLib.Extensions;

// 补丁异步方法（异步方法需要补丁其状态机的 MoveNext）
harmony.PatchAsyncMoveNext(
    asyncMethod,
    prefix: new HarmonyMethod(typeof(MyPatch), "Prefix"),
    postfix: new HarmonyMethod(typeof(MyPatch), "Postfix")
);

// 获取异步方法的状态机类型
harmony.PatchAsyncMoveNext(asyncMethod, out Type stateMachineType, ...);
```

### PublicPropExtensions

```csharp
using BaseLib.Extensions;

// 检查是否为有能量的攻击
bool isPoweredAttack = props.IsPoweredAttack_();

// 检查是否为卡牌或怪物移动
bool isMove = props.IsCardOrMonsterMove_();
```

### MethodInfoExtensions

```csharp
using BaseLib.Extensions;

// 获取异步方法的状态机类型
Type stateMachineType = asyncMethod.StateMachineType();
```

### TypeExtensions

```csharp
using BaseLib.Extensions;

// 在状态机类中查找指定名称的字段
FieldInfo field = stateMachineType.FindStateMachineField("myVariable");
// 查找名为 "<myVariable>5__2" 或 "myVariable" 的字段
```

## GodotUtils

用于处理 Godot 节点和场景：

```csharp
using BaseLib.Utils;

// 从场景创建生物视觉节点
var visuals = GodotUtils.CreatureVisualsFromScene("res://scenes/creature_visuals/my_character.tscn");

// 转移节点（扩展方法）
var node = new MyNode().TransferAllNodes("res://scenes/my_scene.tscn", "Node1", "Node2");
```

### 方法说明

| 方法 | 说明 |
|------|------|
| `CreatureVisualsFromScene(string path)` | 从场景创建生物视觉节点 |
| `TransferAllNodes<T>(this T, string, params string[])` | 从源场景转移指定节点到目标节点 |

### TransferAllNodes 详细说明

```csharp
// 从场景转移节点到当前节点
var customNode = new CustomNode()
    .TransferAllNodes("res://scenes/Template.tscn", "Visuals", "Bounds", "IntentPos");
```

**功能**：
- 设置目标节点名称
- 转移指定的子节点
- 设置 `UniqueNameInOwner` 属性
- 递归设置所有子节点的 Owner
- 记录缺失的必需节点（警告日志）
- 释放源节点

## 常用命令 (Cmd)

游戏提供了多种命令类用于执行游戏动作：

```csharp
using MegaCrit.Sts2.Core.Commands;

// PowerCmd - 能力相关命令
await PowerCmd.Apply<TPower>(target, amount, source, card);  // 施加能力
await PowerCmd.Apply<TPower>(targets, amount, source, card); // 批量施加能力

// CreatureCmd - 生物相关命令
await CreatureCmd.GainBlock(creature, blockVar, cardPlay);   // 获得格挡
await CreatureCmd.GainBlock(creature, amount, valueProp, cardPlay); // 获得格挡（指定属性）
await CreatureCmd.Heal(creature, amount);                    // 治疗
await CreatureCmd.Damage(context, creature, amount, valueProp, source); // 造成伤害
await CreatureCmd.LoseBlock(creature, amount);               // 失去格挡
await CreatureCmd.TriggerAnim(creature, animName, delay);    // 触发动画
await CreatureCmd.GainMaxHp(creature, amount);               // 获得最大生命
await CreatureCmd.LoseMaxHp(context, creature, amount, isFromCard); // 失去最大生命
await CreatureCmd.SetMaxHp(creature, newMaxHp);              // 设置最大生命

// PlayerCmd - 玩家相关命令
await PlayerCmd.GainEnergy(amount, player);                  // 获得能量
await PlayerCmd.LoseGold(amount, player);                    // 失去金币
PlayerCmd.EndTurn(player, canBackOut: false);                // 结束回合

// CardPileCmd - 卡牌堆相关命令
await CardPileCmd.Draw(context, count, player);              // 抽牌
await CardPileCmd.AddGeneratedCardsToCombat(cards, pileType, addedByPlayer); // 添加生成的卡牌

// CardCmd - 卡牌相关命令
CardCmd.Upgrade(card);                                        // 升级卡牌

// RelicCmd - 遗物相关命令
await RelicCmd.Obtain(relic, player);                        // 获得遗物

// RewardsCmd - 奖励相关命令
await RewardsCmd.OfferCustom(player, rewards);               // 提供自定义奖励
```

## ShaderUtils

用于生成和处理着色器：

```csharp
using BaseLib.Utils;

var material = ShaderUtils.GenerateHsv(hue, saturation, value);
```

## WeightedList

用于创建加权随机列表：

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Random;

var weightedList = new WeightedList<string>();
weightedList.Add("Option 1", 1);
weightedList.Add("Option 2", 2);

var selected = weightedList.GetRandom(rng);

var selectedAndRemove = weightedList.GetRandom(rng, remove: true);

weightedList.Insert(0, "Option 0", 3);

var count = weightedList.Count;
```

**实现接口**：
- `IList<T>`：支持列表操作
- `IWeighted`：支持权重接口

## AncientDialogueUtil

用于处理先古之民对话本地化：

```csharp
using BaseLib.Utils;

// 获取音效路径
string sfxPath = AncientDialogueUtil.SfxPath("MYMOD-MY_ANCIENT.talk.firstvisitEver.0-0.ancient");

// 生成本地化基础键
string baseKey = AncientDialogueUtil.BaseLocKey("MY_ANCIENT", "Ironclad"); // "MY_ANCIENT.talk.Ironclad."

// 获取对话列表
var dialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", baseKey, log);
```

**方法说明**：
- `SfxPath(string dialogueLoc)`：根据对话本地化键获取音效路径
- `BaseLocKey(string ancientId, string charId)`：生成角色对话的基础本地化键
- `GetDialoguesForKey(string locTable, string baseKey, StringBuilder? log)`：获取指定键的所有对话

## OptionPools

用于构建先古之民的选项池：

```csharp
using BaseLib.Utils;

// 使用三个独立池（每个选项一个池）
var pools = new OptionPools(pool1, pool2, pool3);

// 使用两个池（前两个选项共用一个池）
var pools = new OptionPools(pool12, pool3);

// 使用单个池（所有选项共用一个池）
var pools = new OptionPools(pool);

// 获取所有选项
var allOptions = pools.AllOptions;

// 随机抽取选项
var selectedOptions = pools.Roll(rng);
```

## AncientOption

先古之民选项抽象类：

```csharp
using BaseLib.Utils;

// 从遗物创建基础选项
var option = (AncientOption)ModelDb.Relic<MyRelic>();

// 创建带权重的选项
var option = new AncientOption<MyRelic>(weight: 2);

// 创建带预处理和变体的选项
var option = new AncientOption<MyRelic>(weight: 1)
{
    ModelPrep = relic => relic.Setup(),
    Variants = relic => new[] { relic, relic.UpgradedVersion }
};
```

**属性说明**：
- `Weight`：选项权重
- `AllVariants`：所有变体遗物
- `ModelForOption`：当前选项对应的遗物模型

## SpireField

用于创建自定义字段（Harmony 补丁），基于 `ConditionalWeakTable` 实现：

```csharp
using BaseLib.Utils;

private static readonly SpireField<Creature, int> MyCustomField = new(() => 0);

MyCustomField.Set(creature, 10);
var value = MyCustomField.Get(creature);

MyCustomField[creature] = 20;
```

**构造函数参数**：
- `defaultVal`：`Func<TVal?>` 或 `Func<TKey, TVal?>` - 获取默认值的函数

**注意**：SpireField 是 `ConditionalWeakTable` 的封装，适用于存储引用类型键的附加数据。值类型会被装箱，效率较低。

## ModelDb 工具

`ModelDb` 是游戏的核心模型数据库，用于获取和注册各种游戏模型：

```csharp
using MegaCrit.Sts2.Core.Models;

// 获取模型实例
var card = ModelDb.Card<MyCard>();
var relic = ModelDb.Relic<MyRelic>();
var power = ModelDb.Power<MyPower>();
var modifier = ModelDb.Modifier<MyModifier>();
var cardPool = ModelDb.CardPool<ColorlessCardPool>();
var act = ModelDb.Act<Hive>();

// 检查模型是否存在
bool exists = ModelDb.Contains(typeof(MyCard));

// 注册自定义类型（需要通过 Harmony 补丁）
ModelDb.Inject(typeof(MyModifier));

// 获取所有模型列表
var allCards = ModelDb.AllCards;
var allRelics = ModelDb.AllRelics;
var allPowers = ModelDb.AllPowers;
var allSharedCardPools = ModelDb.AllSharedCardPools;
```

**常用方法**：
- `ModelDb.Card<T>()`：获取卡牌模型
- `ModelDb.Relic<T>()`：获取遗物模型
- `ModelDb.Power<T>()`：获取能力模型
- `ModelDb.Modifier<T>()`：获取修改器模型
- `ModelDb.CardPool<T>()`：获取卡牌池
- `ModelDb.Act<T>()`：获取章节模型
- `ModelDb.Contains(Type)`：检查类型是否已注册
- `ModelDb.Inject(Type)`：注入自定义类型

**注册自定义模型**：

对于非 BaseLib 管理的自定义模型（如 Modifier），需要通过 Harmony 补丁注册：

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class CustomModelRegistrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ModelDb.Contains(typeof(MyModifier)))
        {
            ModelDb.Inject(typeof(MyModifier));
            MainFile.Logger.Info("MyModifier registered to ModelDb");
        }
    }
}
```

## IL 补丁工具 (Patching)

BaseLib 提供了 IL 指令匹配和修补工具，简化 Transpiler 编写：

### InstructionMatcher

流式 API 匹配 IL 指令序列：

```csharp
using BaseLib.Utils.Patching;
using HarmonyLib;

// 创建匹配器
var matcher = new InstructionMatcher(codeInstructions);

// 匹配指令序列
matcher
    .Match(OpCodes.Ldarg_0)
    .Match(OpCodes.Call, AccessTools.Method(typeof(SomeClass), "SomeMethod"))
    .Match(OpCodes.Stloc_0);

// 获取匹配位置
int position = matcher.Pos;
```

### InstructionPatcher

IL 指令修补器：

```csharp
using BaseLib.Utils.Patching;
using HarmonyLib;

public static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions)
{
    var patcher = new InstructionPatcher(instructions);

    // 查找并替换
    while (patcher.Find(
        new IMatcher[] { InstructionMatcher.OpCode(OpCodes.Call, someMethod) }))
    {
        // 获取标签
        patcher.GetLabels(out var labels);

        // 替换指令
        patcher.Replace(new CodeInstruction(OpCodes.Call, myMethod).WithLabels(labels));
    }

    return patcher;
}
```

**InstructionPatcher 关键方法**：

| 方法 | 描述 |
|------|------|
| `Find(params IMatcher[])` | 查找匹配的指令序列 |
| `Step(int amt)` | 移动位置 |
| `GetLabels(out List<Label>)` | 获取当前位置的标签 |
| `Insert(IEnumerable<CodeInstruction>)` | 插入指令 |
| `Replace(CodeInstruction)` | 替换当前指令 |
| `Remove()` | 移除当前指令 |

### IMatcher 接口

自定义指令匹配器：

```csharp
public interface IMatcher
{
    bool Matches(CodeInstruction instruction);
}
```

**内置匹配器**：
- `InstructionMatcher.OpCode(opCode)`：匹配操作码
- `InstructionMatcher.OpCode(opCode, operand)`：匹配操作码和操作数
- `InstructionMatcher.Call(method)`：匹配方法调用

### HarmonyExtensions

用于补丁异步方法的扩展：

```csharp
using BaseLib.Extensions;

// 补丁异步方法
harmony.PatchAsyncMoveNext(typeof(MyClass), "MyAsyncMethod");
```

## GeneratedNodePool

自定义节点池工具，用于不使用场景文件的池化对象：

```csharp
using BaseLib.Utils;

public class MyPooledNode : Node
{
    public static readonly GeneratedNodePool<MyPooledNode> Pool = new(() => new MyPooledNode());

    public void Reset()
    {
        // 重置节点状态
    }
}

// 使用
var node = MyPooledNode.Pool.Get();
// ... 使用节点 ...
MyPooledNode.Pool.Return(node);
```
