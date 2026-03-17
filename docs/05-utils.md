# 工具类

## GodotUtils

用于处理 Godot 节点和场景：

```csharp
using BaseLib.Utils;

var visuals = GodotUtils.CreatureVisualsFromScene("res://scenes/creature_visuals/my_character.tscn");

var node = new MyNode().TransferAllNodes("res://scenes/my_scene.tscn", "Node1", "Node2");
```

**方法说明**：
- `CreatureVisualsFromScene(string path)`：从场景创建生物视觉节点
- `TransferAllNodes<T>(this T obj, string sourceScene, params string[] uniqueNames)`：转移节点

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

**攻击命令目标类型**：
- `TargetType.AnyEnemy`：单个敌人
- `TargetType.AllEnemies`：所有敌人
- `TargetType.RandomEnemy`：随机敌人

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
