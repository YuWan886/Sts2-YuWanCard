# 项目设置

## 引用 BaseLib

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

**BaseLib 核心功能**：
- `CustomCardModel`：自定义卡牌基类
- `CustomCharacterModel`：自定义角色基类
- `CustomRelicModel`：自定义遗物基类
- `CustomPowerModel`：自定义能力基类
- `CustomPotionModel`：自定义药水基类
- `CustomAncientModel`：自定义先古之民基类
- `PoolAttribute`：内容池属性标记
- `CommonActions`：常用游戏动作工具
- `GodotUtils`：Godot 节点和场景处理工具
- `ShaderUtils`：着色器生成工具
- `WeightedList`：加权随机列表
- `SpireField`：Harmony 自定义字段

## 基本结构

推荐的项目结构：

```
YourMod/
├── Abstracts/         # 自定义抽象类（如果需要）
├── Ancients/          # 先古之民事件定义
├── Cards/             # 卡牌定义
├── Characters/        # 角色定义
├── Modifiers/         # 修改器定义（如无尽模式）
├── Relics/            # 遗物定义
├── Powers/            # 能力定义
├── Potions/           # 药水定义
├── Config/            # 配置相关
├── Patches/           # Harmony 补丁
├── Utils/             # 工具类
├── MainFile.cs        # 模组入口
├── YourMod.json       # 模组元数据
└── project.godot      # Godot 项目文件

YourMod/               # 模组资源目录
├── images/
│   ├── card_portraits/    # 卡牌立绘
│   ├── powers/            # 能力图标
│   ├── relics/            # 遗物图标
│   ├── ancients/          # 先古之民图标和背景
│   ├── modifiers/         # 修改器图标
│   └── ui/run_history/    # UI 图标
├── scenes/
│   └── ancients/          # 先古之民场景
├── localization/
│   ├── zhs/               # 简体中文
│   │   ├── cards.json
│   │   ├── powers.json
│   │   ├── relics.json
│   │   ├── ancients.json
│   │   └── modifiers.json
│   └── eng/               # 英文
└── mod_image.png          # 模组图标
```

## PoolAttribute 属性

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

## ICustomEnergyIconPool 接口

`ICustomEnergyIconPool` 接口用于为自定义卡牌池添加自定义能量图标：

```csharp
using BaseLib.Abstracts;

public class MyCardPool : CustomCardPoolModel, ICustomEnergyIconPool
{
    public string? BigEnergyIconPath => "res://MyMod/images/ui/energy_big.png";
    public string? TextEnergyIconPath => "res://MyMod/images/ui/energy_text.png";
    public string? EnergyColorName => "my_custom_energy";
}
```

**属性说明**：
| 属性 | 说明 |
|------|------|
| `BigEnergyIconPath` | 大能量图标路径 |
| `TextEnergyIconPath` | 文本能量图标路径 |
| `EnergyColorName` | 能量颜色名称 |

## ICustomModel 接口

`ICustomModel` 是一个标记接口，用于确定是否需要添加模组前缀到 ID。BaseLib 会自动为所有实现此接口的模型添加模组前缀，确保不同模组的内容不会冲突。

**自动实现 ICustomModel 的基类**：
- `CustomCardModel`
- `CustomCharacterModel`
- `CustomRelicModel`
- `CustomPowerModel`（通过 `ICustomPower`）
- `CustomPotionModel`
- `CustomAncientModel`
- `CustomCardPoolModel`
- `CustomRelicPoolModel`
- `CustomPotionPoolModel`
- `CustomPile`
- `PlaceholderCharacterModel`

**前缀生成规则**：前缀基于类型的命名空间生成。

## ICustomPower 接口

`ICustomPower` 接口用于为能力类提供自定义图标路径。如果你的能力需要继承自其他能力类（而不是直接继承 `PowerModel`），可以实现此接口：

```csharp
using BaseLib.Abstracts;

public class MyCustomPower : SomeOtherPower, ICustomPower
{
    public string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png";
    public string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";
    public string? CustomBigBetaIconPath => null;
}
```

**属性说明**：
| 属性 | 说明 |
|------|------|
| `CustomPackedIconPath` | 小图标路径（64x64 像素） |
| `CustomBigIconPath` | 大图标路径（256x256 像素） |
| `CustomBigBetaIconPath` | Beta 版大图标路径（256x256 像素） |

**说明**：`CustomPowerModel` 同时继承了 `PowerModel` 和 `ICustomPower`，适合大多数情况。`ICustomPower` 接口适合需要继承其他能力类的情况。

## PlaceholderCharacterModel

`PlaceholderCharacterModel` 是一个占位角色模型，使用现有角色的资源：

```csharp
using BaseLib.Abstracts;

public class MyPlaceholderCharacter : PlaceholderCharacterModel
{
    public MyPlaceholderCharacter() : base(
        baseCharacter: ModelDb.Character<Ironclad>(),
        name: "My Character"
    )
    {
        StartingHealth = 70;
        StartingGold = 99;
    }
}
```

**用途**：
- 快速创建使用现有角色视觉的自定义角色
- 测试和原型开发
- 不需要创建新视觉资源的情况

## CustomPile

`CustomPile` 是自定义牌堆基类：

```csharp
using BaseLib.Abstracts;

public class MyCustomPile : CustomPile
{
    public MyCustomPile(Player player) : base(player)
    {
    }

    public override string PileName => "My Custom Pile";
}
```

**用途**：
- 创建特殊的卡牌存储区域
- 实现自定义的卡牌管理逻辑
