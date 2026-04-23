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
- `ConstructedCardModel`：链式 API 卡牌基类（推荐）
- `CustomCharacterModel`：自定义角色基类
- `CustomRelicModel`：自定义遗物基类
- `CustomPowerModel`：自定义能力基类
- `CustomPotionModel`：自定义药水基类
- `CustomAncientModel`：自定义先古之民基类
- `CustomMonsterModel`：自定义怪物基类
- `CustomEncounterModel`：自定义遭遇基类
- `CustomPetModel`：自定义宠物基类
- `CustomCardPoolModel`：自定义卡牌池基类
- `CustomRelicPoolModel`：自定义遗物池基类
- `CustomPotionPoolModel`：自定义药水池基类
- `CustomSingletonModel`：持续接收钩子的单例模型基类
- `CustomOrbModel`：自定义球体基类
- `CustomPile`：自定义牌堆基类
- `PoolAttribute`：内容池属性标记
- `CommonActions`：常用游戏动作工具（支持 `CalculatedDamageVar` 优先）
- `NodeFactory<T>`：节点工厂（推荐，替代 GodotUtils）
- `GodotUtils`：Godot 节点和场景处理工具（部分已弃用）
- `ShaderUtils`：着色器生成工具
- `WeightedList`：加权随机列表
- `SpireField`：Harmony 自定义字段
- `SavedProperty`：自动持久化属性（使用 `GetProperties` 检查）

## 项目配置 (csproj 设置)

BaseLib 提供了自动化的项目配置系统，可以自动检测游戏路径。

### Sts2PathDiscovery.props

BaseLib 包含 `Sts2PathDiscovery.props` 文件，可以自动检测不同操作系统上的游戏安装路径：

**Windows**:
1. 首先尝试从注册表读取 Steam 卸载位置（App ID: 2868840）
2. 然后尝试 `%SteamPath%\steamapps`
3. 最后回退到 `C:\Program Files (x86)\Steam\steamapps`

**Linux**:
- 默认路径：`~/.local/share/Steam/steamapps`

**macOS**:
- 默认路径：`~/Library/Application Support/Steam/steamapps`

**自动设置的属性**：

| 属性 | 说明 |
|------|------|
| `SteamLibraryPath` | Steam 库路径 |
| `Sts2Path` | 游戏安装路径 |
| `Sts2DataDir` | 游戏数据目录（包含托管程序集） |
| `ModsPath` | 模组输出路径 |

### local.props 本地配置

创建 `local.props` 文件（已被 .gitignore 忽略）来覆盖默认路径：

```xml
<Project>
    <PropertyGroup>
        <!-- 游戏安装路径 -->
        <Sts2Path>Path\To\SteamLibrary\steamapps\common\Slay the Spire 2</Sts2Path>
        
        <!-- 可选：覆盖托管数据文件夹 -->
        <!-- <Sts2DataDir>$(Sts2Path)\data_sts2_windows_x86_64</Sts2DataDir> -->
        
        <!-- 可选：MegaDot / Godot 4.5.1 mono 可执行文件路径（用于 --export-pack） -->
        <!-- <GodotPath>Z:\Projects\sts2\megadot\MegaDot_v4.5.1-stable_mono_win64.exe</GodotPath> -->
    </PropertyGroup>
</Project>
```

### 在 csproj 中导入

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <!-- 其他配置 -->
    </PropertyGroup>

    <!-- 导入 BaseLib 的路径发现配置 -->
    <Import Project="path\to\BaseLib-StS2-master\Sts2PathDiscovery.props" />
    
    <!-- 可选：导入本地配置覆盖 -->
    <Import Project="local.props" Condition="Exists('local.props')" />

    <!-- 使用自动检测的路径 -->
    <ItemGroup>
        <Reference Include="sts2">
            <HintPath>$(Sts2DataDir)\sts2.dll</HintPath>
        </Reference>
    </ItemGroup>
</Project>
```

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
├── YuWanCardCode/               # 模组源代码目录
│   ├── Ancients/                # 先古之民定义
│   │   └── PigPig.cs            # 猪猪先古之民
│   ├── Cards/                   # 卡牌定义（50+ 张卡牌）
│   │   ├── YuWanCardModel.cs    # 卡牌基类
│   │   ├── PigStrike.cs         # 猪打击
│   │   ├── PigAngry.cs          # 猪愤怒
│   │   ├── RainDark.cs          # 雨落狂流之暗
│   │   └── ...
│   ├── Characters/              # 角色定义
│   │   ├── Pig.cs               # 猪角色
│   │   ├── PigCardPool.cs       # 猪卡牌池
│   │   ├── PigRelicPool.cs      # 猪遗物池
│   │   └── PigPotionPool.cs     # 猪药水池
│   ├── Powers/                  # 能力定义（12 个能力）
│   │   ├── YuWanPowerModel.cs   # 能力基类
│   │   ├── PigDoubtPower.cs     # 猪疑惑
│   │   ├── RainDarkPower.cs     # 雨落狂流
│   │   └── ...
│   ├── Relics/                  # 遗物定义（18 个遗物）
│   │   ├── YuWanRelicModel.cs   # 遗物基类
│   │   ├── RingOfSevenCurses.cs # 七咒之戒
│   │   ├── TenYearBamboo.cs     # 10 年孤竹
│   │   └── ...
│   ├── Modifiers/               # 修改器定义
│   │   ├── YuWanModifierModel.cs # 修改器基类
│   │   ├── EndlessModifier.cs   # 无尽模式
│   │   └── VakuuTowerModifier.cs # Vakuu塔
│   ├── Monsters/                # 怪物定义
│   │   ├── YuWanMonsterModel.cs # 怪物基类
│   │   ├── Killer.cs            # 杀手精英怪
│   │   └── PigMinion.cs         # 猪随从
│   ├── Encounters/              # 遭遇定义
│   │   └── KillerElite.cs       # 杀手遭遇
│   ├── Enchantments/            # 附魔定义
│   │   ├── ArthropodKiller.cs   # 节肢动物杀手
│   │   ├── Loyal.cs             # 忠诚
│   │   └── ...
│   ├── Events/                  # 事件定义
│   │   └── Blacksmith.cs        # 铁匠事件
│   ├── Orbs/                    # 能量球定义
│   │   └── LittleCrownPrinceOrb.cs # 小王子能量球
│   ├── GameActions/             # 自定义游戏动作
│   │   └── BugPigAction.cs      # 虫猪动作
│   ├── Multiplayer/             # 多人游戏相关
│   │   ├── TeammatePayMessageHandler.cs
│   │   └── TeammatePayMessages.cs
│   ├── RestSite/                # 休息处选项
│   │   └── RoastPorkRestSiteOption.cs
│   ├── UI/                      # UI 组件
│   │   ├── ShoppingCartPopup.cs
│   │   └── ...
│   ├── Patches/                 # Harmony 补丁
│   │   ├── NeowSevenCursesPatch.cs
│   │   ├── KillerRegistrationPatch.cs
│   │   └── ...
│   ├── Utils/                   # 工具类
│   │   ├── GameVersionCompat.cs # 版本兼容性
│   │   ├── PowerSafetyUtils.cs  # 能力安全性检查
│   │   └── ...
│   ├── Vfx/                     # 视觉效果
│   │   └── VfxGlitchController.cs
│   ├── Config/                  # 配置
│   │   └── YuWanCardConfig.cs
│   └── Commands/                # 调试命令
│       └── YwDebugCmd.cs
└── YuWanCard/                   # 资源目录
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
