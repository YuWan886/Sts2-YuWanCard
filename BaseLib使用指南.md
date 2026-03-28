# BaseLib 使用指南

BaseLib 是一个为 Slay the Spire 2 (StS2) 模组开发提供基础功能的库，它简化了模组开发过程，提供了各种抽象类和工具来帮助开发者创建自定义内容。

## 文档目录

### 入门指南

1. [项目设置](docs/01-project-setup.md) - 如何设置项目、引用 BaseLib、项目结构
2. [核心功能](docs/02-core-features.md) - 卡牌、角色、遗物、能力、药水、先古之民等核心功能
3. [配置系统](docs/03-config-system.md) - 模组配置和 SavedProperty 属性

### 进阶功能

4. [自定义 Modifier](docs/04-custom-modifier.md) - 创建自定义游戏模式修改器
5. [工具类](docs/05-utils.md) - GodotUtils、CommonActions、ModelDb 等工具
6. [自定义动态变量](docs/06-custom-variables.md) - PersistVar、RefundVar、ExhaustiveVar

### 参考资料

7. [BBCode 与占位符](docs/07-bbcode-and-placeholders.md) - BBCode 标签、占位变量、Formatter 格式化器
8. [最佳实践](docs/08-best-practices.md) - 命名约定、调试、性能优化
9. [示例代码](docs/09-examples.md) - 完整的模组示例代码
10. [故障排除](docs/10-troubleshooting.md) - 常见问题和解决方案
11. [扩展功能](docs/11-extensions.md) - 自定义变量、遗物升级等扩展功能

## 快速开始

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyFirstCard : CustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m)];

    public MyFirstCard() : base(
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
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

## 核心概念

### 抽象基类

| 基类 | 用途 |
|------|------|
| `CustomCardModel` | 自定义卡牌基类 |
| `CustomCharacterModel` | 自定义角色基类 |
| `CustomRelicModel` | 自定义遗物基类 |
| `CustomPowerModel` | 自定义能力基类 |
| `CustomPotionModel` | 自定义药水基类 |
| `CustomAncientModel` | 自定义先古之民基类 |
| `CustomCardPoolModel` | 自定义卡牌池基类 |
| `CustomRelicPoolModel` | 自定义遗物池基类 |
| `CustomPotionPoolModel` | 自定义药水池基类 |

### 接口

| 接口 | 用途 |
|------|------|
| `ICustomModel` | 标记接口，用于确定是否需要添加模组前缀到 ID |
| `ICustomPower` | 自定义能力接口，可与其他能力类一起继承 |
| `IHealAmountModifier` | 治疗量修改器接口 |

### 工具类

| 类 | 用途 |
|------|------|
| `PoolAttribute` | 内容池属性标记，用于将自定义内容注册到正确的池 |
| `CommonActions` | 常用游戏动作工具（攻击、格挡、抽牌、施加能力等） |
| `ModelDb` | 游戏模型数据库，用于获取和注册各种游戏模型 |
| `GodotUtils` | Godot 节点和场景处理工具 |
| `ShaderUtils` | 着色器生成工具 |
| `WeightedList` | 加权随机列表 |
| `SpireField` | Harmony 自定义字段（基于 ConditionalWeakTable） |
| `AncientDialogueUtil` | 先古之民对话本地化工具 |
| `OptionPools` | 先古之民选项池构建工具 |
| `AncientOption` | 先古之民选项抽象类 |

### 自定义动态变量

| 变量 | 用途 |
|------|------|
| `PersistVar` | 持续次数（每回合可打出 X 次） |
| `RefundVar` | 能量返还（打出后返还 X 点能量） |
| `ExhaustiveVar` | 耗尽次数（本场战斗总共可打出 X 次，至少保留 1 次） |

### 配置系统

| 类 | 用途 |
|------|------|
| `ModConfig` | 模组配置基类 |
| `ModConfigRegistry` | 配置注册表 |
| `SavedProperty` | 存档属性标记，用于持久化保存属性 |

## 重要特性

### 自动注册机制

继承自 `ICustomModel` 的类在构造时会自动注册到对应的内容池：

- `CustomCardModel`：自动添加到 `PoolAttribute` 指定的卡牌池
- `CustomRelicModel`：自动添加到 `PoolAttribute` 指定的遗物池
- `CustomPotionModel`：自动添加到 `PoolAttribute` 指定的药水池
- `CustomAncientModel`：自动添加到先古之民列表
- `CustomCardPoolModel`：如果 `IsShared` 为 true，自动注册到共享卡牌池列表

### ID 前缀系统

BaseLib 会自动为所有实现 `ICustomModel` 接口的模型添加模组前缀，确保不同模组的内容不会冲突。前缀基于类型的命名空间生成。

### 格挡自动检测

`CustomCardModel` 的 `GainsBlock` 属性会自动检测 `DynamicVars` 中是否包含 `BlockVar` 或 `CalculatedBlockVar`，无需手动设置。

### 自定义图标路径

所有自定义模型都支持通过属性指定自定义图标路径：

```csharp
// 卡牌
public override string? CustomPortraitPath => "res://MyMod/images/card_portraits/my_card.png";
public override Texture2D? CustomFrame => GD.Load<Texture2D>("res://MyMod/images/card_frames/my_frame.png");

// 能力
public override string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png"; // 64x64
public override string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";    // 256x256

// 遗物
public override string? PackedImagePath => "res://MyMod/images/relics/my_relic.png";
public override string? PackedOutlinePath => "res://MyMod/images/relics/my_relic_outline.png";

// 药水
public override string? PackedImagePath => "res://MyMod/images/potions/my_potion.png";
public override string? PackedOutlinePath => "res://MyMod/images/potions/my_potion_outline.png";

// 先古之民
public override string? CustomScenePath => "res://MyMod/scenes/ancients/my_ancient.tscn";
public override string? CustomMapIconPath => "res://MyMod/images/ancients/my_ancient.png";
public override Texture2D? CustomRunHistoryIcon => GD.Load<Texture2D>("res://MyMod/images/ui/run_history/my_ancient.png");
```

### DynamicVar 扩展方法

BaseLib 提供了 `DynamicVarExtensions` 类，包含以下扩展方法：

```csharp
// 为动态变量添加提示框
var myVar = new MyCustomVar(5m).WithTooltip();

// 计算格挡值（考虑各种加成）
decimal block = blockVar.CalculateBlock(creature, ValueProp.None, cardPlay, card);
```

`WithTooltip()` 方法会自动从 `static_hover_tips` 本地化表中读取提示文本，键名格式为 `{PREFIX}-{VAR_NAME}.title` 和 `{PREFIX}-{VAR_NAME}.description`。

## 相关链接

- [BaseLib 项目](https://github.com/Alchyr/BaseLib-StS2)
