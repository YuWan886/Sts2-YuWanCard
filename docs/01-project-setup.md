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
- `CommonActions`：常用游戏动作工具（支持 `CalculatedDamageVar` 优先）
- `GodotUtils`：Godot 节点和场景处理工具
- `ShaderUtils`：着色器生成工具
- `WeightedList`：加权随机列表
- `SpireField`：Harmony 自定义字段
- `SavedProperty`：自动持久化属性（使用 `GetProperties` 检查）

## 基本结构

推荐的项目结构：

```
YourMod/
├── .godot/                    # Godot 引擎配置目录
├── .template.config/          # 模板配置
├── .vscode/                   # VSCode 配置
├── packages/                  # NuGet 包目录
├── YourMod/                   # 模组资源目录
│   ├── images/
│   │   ├── card_portraits/    # 卡牌立绘
│   │   ├── powers/            # 能力图标
│   │   ├── relics/            # 遗物图标
│   │   ├── ancients/          # 先古之民图标和背景
│   │   ├── modifiers/         # 修改器图标
│   │   └── ui/run_history/    # UI 图标
│   ├── localization/zhs/      # 简体中文本地化
│   │   ├── cards.json         # 卡牌本地化
│   │   ├── powers.json        # 能力本地化
│   │   ├── relics.json        # 遗物本地化
│   │   ├── ancients.json      # 先古之民本地化
│   │   └── modifiers.json     # 修改器本地化
│   └── mod_image.png          # 模组图标
├── YourModCode/               # 模组源代码目录
│   ├── Cards/                 # 卡牌定义
│   │   ├── xxx.cs             # xxxxx 卡牌
│   │   └── YourModCardModel.cs # 卡牌基类
│   ├── Powers/                # 能力定义
│   │   ├── xxx.cs             # xxxxx 能力
│   │   └── YourModPowerModel.cs # 能力基类
│   ├── Relics/                # 遗物定义
│   │   ├── xxx.cs             # xxxxx
│   │   └── YourModRelicModel.cs # 遗物基类
│   ├── Ancients/              # 先古之民定义
│   ├── Modifiers/             # 修改器定义
│   ├── Monsters/              # 怪物定义
│   ├── Encounters/            # 遭遇定义
│   ├── Patches/               # Harmony 补丁
│   └── Utils/                 # 工具类
├── others/                    # 参考资源目录
├── MainFile.cs                # 模组入口文件
├── YourMod.csproj             # 项目配置文件
├── YourMod.json               # 模组清单文件
└── AGENTS.md                  # AI 开发指南
```

**YuWanCard 项目结构示例**：

```
YuWanCard/
├── YuWanCardCode/
│   ├── Cards/                 # 20+ 张卡牌定义
│   │   ├── YuWanCardModel.cs  # 卡牌基类
│   │   ├── PigHurt.cs         # 猪受伤
│   │   ├── PigAngry.cs        # 猪愤怒
│   │   ├── RainDark.cs        # 雨落狂流之暗
│   │   └── ...
│   ├── Powers/                # 6 个能力定义
│   │   ├── YuWanPowerModel.cs # 能力基类
│   │   ├── PigDoubtPower.cs   # 猪疑惑
│   │   ├── RainDarkPower.cs   # 雨落狂流
│   │   └── ...
│   ├── Relics/                # 13 个遗物定义
│   │   ├── YuWanRelicModel.cs # 遗物基类
│   │   ├── RingOfSevenCurses.cs # 七咒之戒
│   │   ├── TenYearBamboo.cs   # 10 年孤竹
│   │   └── ...
│   ├── Ancients/
│   │   └── PigPig.cs          # 猪猪先古之民
│   ├── Modifiers/
│   │   └── EndlessModifier.cs # 无尽模式
│   ├── Monsters/
│   │   └── Killer.cs          # 杀手精英怪
│   ├── Encounters/
│   │   └── KillerElite.cs     # 杀手遭遇
│   └── Patches/               # Harmony 补丁
│       ├── NeowSevenCursesPatch.cs
│       ├── KillerRegistrationPatch.cs
│       └── ...
└── YuWanCard/                 # 资源目录
    ├── images/
    ├── localization/zhs/
    └── ...
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
