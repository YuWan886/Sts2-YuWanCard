# BaseLib 使用指南

BaseLib 是一个为 Slay the Spire 2 (StS2) 模组开发提供基础功能的库，它简化了模组开发过程，提供了各种抽象类和工具来帮助开发者创建自定义内容。

## 1. 项目设置

### 1.1 引用 BaseLib

1. 将 BaseLib 项目添加到你的解决方案中
2. 在你的模组项目中添加对 BaseLib 的引用
3. 确保你的模组的 `mod_manifest.json` 文件中包含 BaseLib 作为依赖

```json
{
  "id": "YourModId",
  "name": "Your Mod Name",
  "version": "1.0.0",
  "dependencies": [
    {
      "id": "BaseLib",
      "version": "1.0.0"
    }
  ]
}
```

### 1.2 基本结构

推荐的项目结构：

```
YourMod/
├── Abstracts/         # 自定义抽象类（如果需要）
├── Cards/             # 卡牌定义
├── Characters/        # 角色定义
├── Relics/            # 遗物定义
├── Powers/            # 能力定义
├── Potions/           # 药水定义
├── Config/            # 配置相关
├── Patches/           # Harmony 补丁
├── Utils/             # 工具类
├── MainFile.cs        # 模组入口
├── mod_manifest.json  # 模组清单
└── project.godot      # Godot 项目文件
```

### 1.3 PoolAttribute 属性

BaseLib 使用 `PoolAttribute` 属性来确定自定义内容应该添加到哪个池中。所有继承自 `ICustomModel` 的自定义模型都需要使用此属性。

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

[Pool(typeof(SharedRelicPool))]
public class MyCustomRelic : CustomRelicModel
{
}
```

常用的池类型：
- **卡牌池**：`SharedCardPool`、`IroncladCardPool`、`SilentCardPool`、`DefectCardPool`、`RegentCardPool`、`NecrobinderCardPool`、`ColorlessCardPool`（无色卡牌）、`TokenCardPool`、`EventCardPool`、`QuestCardPool`、`StatusCardPool`、`CurseCardPool`
- **遗物池**：`SharedRelicPool`、`IroncladRelicPool`、`SilentRelicPool`、`DefectRelicPool`、`RegentRelicPool`、`NecrobinderRelicPool`、`EventRelicPool`
- **药水池**：`SharedPotionPool`、`IroncladPotionPool`、`SilentPotionPool`、`DefectPotionPool`、`RegentPotionPool`、`NecrobinderPotionPool`、`EventPotionPool`、`TokenPotionPool`

**注意**：使用卡牌池类型时需要引入命名空间 `MegaCrit.Sts2.Core.Models.CardPools`。

## 2. 核心功能

### 2.1 自定义卡牌 (CustomCardModel)

继承 `CustomCardModel` 来创建自定义卡牌：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyCustomCard : CustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m)];

    public MyCustomCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.Enemy
    )
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attackCmd = CommonActions.CardAttack(this, cardPlay, hitCount: 1);
        await choiceContext.RunCommand(attackCmd);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
```

**构造函数参数说明**：
- `baseCost`：基础费用
- `type`：卡牌类型（Attack、Skill、Power、Status、Curse、Quest）
- `rarity`：稀有度（Common、Uncommon、Rare、Ancient）
- `target`：目标类型（Enemy、AllEnemies、RandomEnemy、AnyEnemy、Self、None、AllAllies、AnyAlly）
- `showInCardLibrary`：是否在卡牌库中显示（默认 true）
- `autoAdd`：是否自动添加到内容字典（默认 true）

**重要属性和方法**：
- `GainsBlock`：自动检测卡牌是否有格挡效果（通过检查 DynamicVars 中是否有 BlockVar 或 CalculatedBlockVar）
- `CustomFrame`：自定义卡牌框贴图（可选）
- `CanonicalVars`：定义卡牌的动态变量（伤害、格挡、能量等）
- `OnPlay`：卡牌打出时的逻辑
- `OnUpgrade`：卡牌升级时的逻辑

**DynamicVar 常用类型**：
- `DamageVar(decimal)`：伤害变量
- `BlockVar(decimal, ValueProp = ValueProp.None)`：格挡变量
- `HealVar(decimal)`：治疗变量
- `EnergyVar(decimal)`：能量变量
- `PowerVar<TPower>(decimal)`：能力层数变量

**CardKeyword 常用类型**：
- `CardKeyword.Exhaust`：消耗
- `CardKeyword.Innate`：固有
- `CardKeyword.Ethereal`：虚无
- 使用 `CanonicalKeywords` 属性返回卡牌关键词
- 使用 `AddKeyword(CardKeyword)` 和 `RemoveKeyword(CardKeyword)` 方法在升级时修改关键词

### 2.2 自定义角色 (CustomCharacterModel)

继承 `CustomCharacterModel` 来创建自定义角色：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;

public class MyCustomCharacter : CustomCharacterModel
{
    public MyCustomCharacter()
    {
        Name = "My Character";
        StartingHealth = 70;
        StartingGold = 99;
        AttackAnimDelay = 0.15f;
        CastAnimDelay = 0.25f;
    }

    public override string? CustomVisualPath => "res://scenes/creature_visuals/my_character.tscn";
    public override string? CustomTrailPath => null;
    public override string? CustomIconTexturePath => null;
    public override string? CustomIconPath => "res://textures/icons/my_character.png";
    public override string? CustomEnergyCounterPath => null;
    public override string? CustomRestSiteAnimPath => null;
    public override string? CustomMerchantAnimPath => null;
    public override string? CustomArmPointingTexturePath => null;
    public override string? CustomArmRockTexturePath => null;
    public override string? CustomArmPaperTexturePath => null;
    public override string? CustomArmScissorsTexturePath => null;

    public override string? CustomCharacterSelectBg => null;
    public override string? CustomCharacterSelectIconPath => null;
    public override string? CustomCharacterSelectLockedIconPath => null;
    public override string? CustomCharacterSelectTransitionPath => null;
    public override string? CustomMapMarkerPath => null;

    public override string? CustomAttackSfx => null;
    public override string? CustomCastSfx => null;
    public override string? CustomDeathSfx => null;

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        if (CustomVisualPath == null) return null;
        return GodotUtils.CreatureVisualsFromScene(CustomVisualPath);
    }

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(
            controller,
            idleName: "idle",
            deadName: "dead",
            deadLoop: false,
            hitName: "hit",
            hitLoop: false,
            attackName: "attack",
            attackLoop: false,
            castName: "cast",
            castLoop: false,
            relaxedName: "relaxed",
            relaxedLoop: true
        );
    }
}
```

**视觉场景要求**：
- 如果不重写 `CustomVisualPath`，则需要在 `res://scenes/creature_visuals/` 目录下创建名为 `class_name.tscn` 的场景文件
- 角色选择背景场景路径：`res://scenes/screens/char_select/char_select_bg_class_name.tscn`
- 场景必须包含以下节点：`Visuals`、`Bounds`、`IntentPos`、`CenterPos`、`OrbPos`、`TalkPos`

**默认值**：
- `StartingGold`：99
- `AttackAnimDelay`：0.15f
- `CastAnimDelay`：0.25f

### 2.3 自定义遗物 (CustomRelicModel)

继承 `CustomRelicModel` 来创建自定义遗物：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

[Pool(typeof(SharedRelicPool))]
public class MyCustomRelic : CustomRelicModel
{
    public MyCustomRelic() : base(autoAdd: true)
    {
        Name = "My Relic";
        Description = "Gain 1 energy at the start of each combat.";
    }

    public override RelicModel? GetUpgradeReplacement() => null;
}
```

### 2.4 自定义能力 (CustomPowerModel)

继承 `CustomPowerModel` 来创建自定义能力：

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

public class MyCustomPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png";
    public override string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
        }
    }
}
```

**图标尺寸**：
- `CustomPackedIconPath`：64x64 像素
- `CustomBigIconPath`：256x256 像素
- `CustomBigBetaIconPath`：256x256 像素（Beta 版本图标）

**PowerType 类型**：
- `PowerType.Buff`：增益效果（绿色）
- `PowerType.Debuff`：减益效果（红色）
- `PowerType.Neutral`：中性效果（蓝色）

**PowerStackType 类型**：
- `PowerStackType.Counter`：层数叠加
- `PowerStackType.Duration`：持续时间
- `PowerStackType.None`：不叠加

**常用事件方法**：
- `AfterSideTurnStart(CombatSide side, CombatState combatState)`：回合开始时触发
- `AfterSideTurnEnd(CombatSide side, CombatState combatState)`：回合结束时触发
- `OnApply(Creature source, int amount)` 能力被应用时触发
- `OnRemove()`：能力被移除时触发

### 2.5 自定义卡牌池 (CustomCardPoolModel)

继承 `CustomCardPoolModel` 来创建自定义卡牌池：

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

    protected override CardModel[] GenerateAllCards() => [];
}
```

**重要说明**：
- 所有卡牌池必须是角色池或共享池，否则无法被找到
- 角色池通过 `CharacterModel.CardPool` 属性获取
- 共享池通过 `ModelDb.AllSharedCardPools` 获取
- `IsShared` 为 true 时，池会自动注册到 `ModelDb.AllSharedCardPools`
- `CustomFrame` 用于自定义卡牌框贴图（Ancient 稀有度忽略此逻辑）
- 如果不重写 `CardFrameMaterialPath`，会自动使用 `ShaderColor` 生成 HSV 着色器材质

### 2.6 自定义药水 (CustomPotionModel)

继承 `CustomPotionModel` 来创建自定义药水：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

[Pool(typeof(SharedPotionPool))]
public class MyCustomPotion : CustomPotionModel
{
    public MyCustomPotion()
    {
        Name = "My Potion";
        Description = "Heal 20 HP.";
    }

    public override bool AutoAdd => true;

    public override string? PackedImagePath => "res://textures/potions/my_potion.png";
    public override string? PackedOutlinePath => "res://textures/potions/my_potion_outline.png";
}
```

**属性说明**：
- `AutoAdd`：是否自动添加到内容字典（默认 true）
- `PackedImagePath`：药水贴图路径
- `PackedOutlinePath`：药水轮廓贴图路径

### 2.7 自定义古代 (CustomAncientModel)

继承 `CustomAncientModel` 来创建自定义古代事件：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;

public class MyCustomAncient : CustomAncientModel
{
    public MyCustomAncient() : base(autoAdd: true)
    {
        Name = "My Ancient";
    }

    public override bool IsValidForAct(ActModel act) => act.ActNumber == 2 || act.ActNumber == 3;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<SomeRelic>(weight: 1),
            AncientOption<AnotherRelic>(weight: 2)
        )
    );

    public override string? CustomScenePath => null;
    public override string? CustomMapIconPath => null;
    public override string? CustomMapIconOutlinePath => null;
    public override Texture2D? CustomRunHistoryIcon => null;
    public override Texture2D? CustomRunHistoryIconOutline => null;

    public static WeightedList<AncientOption> MakePool(params RelicModel[] options)
    {
        return CustomAncientModel.MakePool(options);
    }

    public static WeightedList<AncientOption> MakePool(params AncientOption[] options)
    {
        return CustomAncientModel.MakePool(options);
    }

    public static AncientOption AncientOption<T>(int weight = 1, Func<T, RelicModel>? relicPrep = null, Func<T, IEnumerable<RelicModel>>? makeAllVariants = null) where T : RelicModel
    {
        return CustomAncientModel.AncientOption<T>(weight, relicPrep, makeAllVariants);
    }
}
```

**重要方法**：
- `IsValidForAct(ActModel act)`：检查古代是否适用于指定章节（建议检查 `act.ActNumber == 2 or 3`）
- `ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)`：是否强制生成此古代（谨慎使用，可能导致模组冲突）
- `MakeOptionPools`：创建选项池（抽象属性，必须实现）

**选项池工具**：
- `MakePool(params RelicModel[] options)`：从遗物模型创建加权列表
- `MakePool(params AncientOption[] options)`：从古代选项创建加权列表
- `AncientOption<T>(int weight, ...)`：创建古代选项（支持遗物预处理和变体）

**本地化**：
古代事件使用特定的本地化键格式：
- 首次访问：`{Id.Entry}.talk.firstvisitEver.0-0.ancient`
- 角色对话：`{Id.Entry}.talk.{CharacterId}.{index}-{line}.ancient` 或 `.char`
- 通用对话：`{Id.Entry}.talk.ANY.{index}-{line}.ancient` 或 `.char`

### 2.8 自定义药水池 (CustomPotionPoolModel)

继承 `CustomPotionPoolModel` 来创建自定义药水池：

```csharp
using BaseLib.Abstracts;

public class MyCustomPotionPool : CustomPotionPoolModel
{
    public MyCustomPotionPool()
    {
        Name = "My Potion Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<PotionModel> GenerateAllPotions() => [];
}
```

### 2.9 自定义遗物池 (CustomRelicPoolModel)

继承 `CustomRelicPoolModel` 来创建自定义遗物池：

```csharp
using BaseLib.Abstracts;

public class MyCustomRelicPool : CustomRelicPoolModel
{
    public MyCustomRelicPool()
    {
        Name = "My Relic Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
```

## 3. 配置系统

BaseLib 提供了一个简单的配置系统，用于管理模组的设置：

### 3.1 创建配置类

```csharp
using BaseLib.Config;
using Godot;

public class MyModConfig : ModConfig
{
    public static bool EnableFeature { get; set; } = true;
    public static DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

    public MyModConfig() : base() { }

    public override void SetupConfigUI(Control optionContainer)
    {
        MakeToggleOption(optionContainer, typeof(MyModConfig).GetProperty(nameof(EnableFeature))!);
        MakeDropdownOption(optionContainer, typeof(MyModConfig).GetProperty(nameof(Difficulty))!);
    }
}

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}
```

**重要说明**：
- 配置属性必须是**静态属性**（`static`）
- 配置属性必须有 `get` 和 `set` 访问器
- 使用 `MakeToggleOption` 创建开关选项
- 使用 `MakeDropdownOption` 创建下拉选项（仅支持枚举类型）

### 3.2 注册配置

在模组初始化时注册配置：

```csharp
using BaseLib.Config;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public static void Initialize()
    {
        ModConfigRegistry.Register("MyMod", new MyModConfig());
    }
}
```

### 3.3 配置文件路径

配置文件默认保存在以下位置：
- Windows: `%LOCALAPPDATA%\.baselib\[ModNamespace]\[ModName].cfg`
- macOS: `~/Library/[ModNamespace]\[ModName].cfg`
- Android/iOS: Godot 用户数据目录

### 3.4 配置变更事件

可以监听配置变更事件：

```csharp
var config = new MyModConfig();
config.ConfigChanged += (sender, args) => {
};
```

### 3.5 手动保存和加载

```csharp
await config.Save();
await config.Load();
config.Changed();
```

## 4. 工具类

### 4.1 GodotUtils

用于处理 Godot 节点和场景：

```csharp
using BaseLib.Utils;

var visuals = GodotUtils.CreatureVisualsFromScene("res://scenes/creature_visuals/my_character.tscn");

var node = new MyNode().TransferAllNodes("res://scenes/my_scene.tscn", "Node1", "Node2");
```

**方法说明**：
- `CreatureVisualsFromScene(string path)`：从场景创建生物视觉节点
- `TransferAllNodes<T>(this T obj, string sourceScene, params string[] uniqueNames)`：转移节点

### 4.2 CommonActions

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

### 4.3 常用命令 (Cmd)

游戏提供了多种命令类用于执行游戏动作：

```csharp
using MegaCrit.Sts2.Core.Commands;

// PowerCmd -能力相关命令
await PowerCmd.Apply<TPower>(target, amount, source, card);  // 施加能力
await PowerCmd.Apply<TPower>(targets, amount, source, card); // 批量施加能力

// CreatureCmd - 生物相关命令
await CreatureCmd.GainBlock(creature, blockVar, cardPlay);   // 获得格挡
await CreatureCmd.GainBlock(creature, amount, valueProp, cardPlay); // 获得格挡（指定属性）
await CreatureCmd.Heal(creature, amount);                    // 治疗
await CreatureCmd.Damage(context, creature, amount, valueProp, source); // 造成伤害
await CreatureCmd.LoseBlock(creature, amount);               // 失去格挡
await CreatureCmd.TriggerAnim(creature, animName, delay);    // 触发动画

// PlayerCmd - 玩家相关命令
await PlayerCmd.GainEnergy(amount, player);                  // 获得能量
PlayerCmd.EndTurn(player, canBackOut: false);                // 结束回合

// CardPileCmd - 卡牌堆相关命令
await CardPileCmd.Draw(context, count, player);              // 抽牌
```

### 4.4 ShaderUtils

用于生成和处理着色器：

```csharp
using BaseLib.Utils;

var material = ShaderUtils.GenerateHsv(hue, saturation, value);
```

### 4.5 WeightedList

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
var totalWeight = weightedList.TotalWeight;
```

**实现接口**：
- `IList<T>`：支持列表操作
- `IWeighted`：支持权重接口

### 4.6 SpireField

用于创建自定义字段（Harmony 补丁）：

```csharp
using BaseLib.Utils;

private static readonly SpireField<int> MyCustomField = new("MyMod_MyCustomField");

MyCustomField.Set(creature, 10);
var value = MyCustomField.Get(creature);
```

## 5. 自定义动态变量

BaseLib 提供了两个自定义动态变量：

### 5.1 PersistVar

表示卡牌的"持续"次数（每回合打出次数限制）：

```csharp
using BaseLib.Cards.Variables;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用
protected override IEnumerable<DynamicVar> CanonicalVars => [new PersistVar(2)];

// 获取剩余次数
int remaining = PersistVar.PersistCount(card, 2);
```

### 5.2 RefundVar

表示卡牌打出后的能量返还：

```csharp
using BaseLib.Cards.Variables;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

// 在 CanonicalVars 中使用
protected override IEnumerable<DynamicVar> CanonicalVars => [new RefundVar(1)];
```

## 6. 最佳实践

### 6.1 命名约定

- 类名：使用 PascalCase
- 方法名：使用 PascalCase
- 属性名：使用 PascalCase
- 字段名：使用 camelCase 或 _camelCase
- 命名空间：使用 PascalCase，通常以模组名称开头

### 6.2 组织代码

- 将不同类型的内容放在不同的文件夹中
- 使用命名空间来组织代码
- 保持代码简洁明了
- 使用部分类（partial classes）来组织大型类

### 6.3 调试

使用 BaseLib 的日志系统：

```csharp
using BaseLib;

MainFile.Logger.Info("Mod initialized");
MainFile.Logger.Warn("Something might be wrong");
MainFile.Logger.Error("An error occurred");
MainFile.Logger.Debug("Detailed debug information");
```

### 6.4 性能

- 避免在游戏循环中做 heavy 操作
- 使用对象池来减少 GC
- 合理使用 Harmony 补丁
- 使用缓存来减少重复计算
- 延迟加载资源和初始化

### 6.5 代码规范

- 使用 XML 文档注释（///）为公共 API 添加说明
- 遵循 C# 编码规范
- 保持方法简洁，每个方法只做一件事
- 使用有意义的变量和方法名

### 6.6 本地化

- 使用游戏的本地化系统
- 为所有用户可见的文本提供本地化支持
- 遵循游戏的本地化命名约定

**本地化文件格式**：
```json
{
  "MODID-CARD_ID.title": "卡牌名称",
  "MODID-CARD_ID.description": "卡牌描述，支持 {DynamicVar:diff()} 等动态变量",
  "MODID-POWER_ID.title": "能力名称",
  "MODID-POWER_ID.description": "能力描述",
  "MODID-POWER_ID.smartDescription": "能力智能描述"
}
```

**本地化键命名规则**：
- 卡牌：`{ModId}-{CardId}.title` / `.description`
- 能力：`{ModId}-{PowerId}.title` / `.description` / `.smartDescription`
- ModId 和 CardId/PowerId 使用大写，用连字符分隔

**描述中的动态变量**：
- `{Damage:diff()}` - 显示伤害值
- `{Block:diff()}` - 显示格挡值
- `{Heal:diff()}` - 显示治疗值
- `{Energy:diff()}` - 显示能量值
- `{Energy:energyIcons()}` - 显示能量图标
- `{PowerName:diff()}` - 显示能力层数
- `{IfUpgraded:show:升级后文本|升级前文本}` - 根据是否升级显示不同文本

**颜色标签**：
- `[gold]文本[/gold]` - 金色文本
- `[red]文本[/red]` - 红色文本
- `[blue]文本[/blue]` - 蓝色文本

## 7. 示例

### 7.1 完整的模组示例

```csharp
// MainFile.cs
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("YuWanCard initialized");
    }
}

// YuWanCardModel.cs (卡牌基类)
using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Cards;

public abstract partial class YuWanCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://YuWanCard/images/card_portraits/{CardId}";

    public override string PortraitPath => $"{PortraitBasePath}.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}

// PigHurt.cs (卡牌示例)
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigHurt : YuWanCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    public PigHurt() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.AllEnemies
    )
    {
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VulnerablePower>(CombatState!.HittableEnemies, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(2);
    }
}

// YuWanPowerModel.cs (能力基类)
using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Powers;

public abstract class YuWanPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/powers/{PowerId}.png";

    public override string? CustomPackedIconPath => IconBasePath;
    public override string? CustomBigIconPath => IconBasePath;
}

// PigDoubtPower.cs (能力示例)
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            int powerCount = Amount;

            for (int i = 0; i < powerCount; i++)
            {
                var randomPower = GetRandomPower();
                if (randomPower != null)
                {
                    await PowerCmd.Apply(randomPower.ToMutable(), Owner, 1, Owner, null);
                }
            }
        }
    }

    private PowerModel? GetRandomPower()
    {
        var rng = Owner.Player?.RunState.Rng;
        if (rng == null) return null;

        var filteredPowers = ModelDb.AllPowers
            .Where(p => !p.IsInstanced && IsSafePower(p))
            .ToList();

        if (filteredPowers.Count == 0) return null;

        return rng.Niche.NextItem(filteredPowers);
    }

    private bool IsSafePower(PowerModel power)
    {
        // 过滤逻辑...
        return true;
    }
}
```

## 8. 故障排除

### 8.1 常见问题

1. **卡牌不显示**：确保设置了 `showInCardLibrary: true`、`autoAdd: true`，并使用了正确的 `PoolAttribute`
2. **角色不显示**：确保设置了正确的视觉路径，或者在 `res://scenes/creature_visuals/` 目录下创建了对应名称的场景文件
3. **配置不生效**：确保在 `SetupConfigUI` 方法中正确添加了配置选项，并且配置属性是静态属性
4. **Harmony 补丁失败**：检查补丁代码是否正确，确保目标方法存在
5. **自定义视觉不加载**：确保场景文件存在且路径正确，检查场景中是否包含必要的节点（Visuals, Bounds, IntentPos, CenterPos, OrbPos, TalkPos）
6. **卡牌池不显示**：确保正确设置了 `IsShared` 属性，角色卡牌池需要通过角色的 `CardPool` 属性引用
7. **配置文件不保存**：确保模组有写入权限，检查文件路径是否正确
8. **PoolAttribute 错误**：确保所有自定义模型都使用了 `PoolAttribute`，并且池类型正确

### 8.2 日志

查看游戏日志来排查问题，BaseLib 的日志会显示在游戏日志中。

游戏日志位置：
- Windows: `C:\Users\[用户名]\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs/godot.log`

### 8.3 调试技巧

- 使用 `MainFile.Logger.Debug()` 输出详细调试信息
- 检查卡牌 ID 和名称映射：在卡牌数据库中查询
- 测试模组：在游戏中运行一局，查看模组功能是否正常
- 检查 Harmony 补丁：确认补丁是否正确应用
- 验证文件路径：确保所有资源路径都是正确的
- 检查依赖项：确保 BaseLib 已正确引用
- 检查 PoolAttribute：确保池类型与模型类型匹配

## 9. 扩展功能

### 9.1 自定义卡牌变量

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

### 9.2 自定义遗物升级

遗物可以设置升级替换：

```csharp
public override RelicModel? GetUpgradeReplacement()
{
    return new MyUpgradedRelic();
}
```

### 9.3 自定义古代选项

创建带有变体的古代选项：

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

## 10. 总结

BaseLib 为 StS2 模组开发提供了一个强大的基础框架，它简化了许多常见的开发任务，使开发者能够更专注于内容创作。通过继承 BaseLib 提供的抽象类并使用 `PoolAttribute` 属性，你可以快速创建自定义的卡牌、角色、遗物、能力和药水，而不需要处理底层的实现细节。

**关键要点**：
- 所有自定义模型都需要使用 `PoolAttribute` 属性
- 配置属性必须是静态属性
- 角色视觉场景需要包含必要的节点
- 古代事件需要实现 `MakeOptionPools` 属性
- 使用 `CommonActions` 简化常见的游戏动作
- 使用 `MainFile.Logger` 进行日志记录
