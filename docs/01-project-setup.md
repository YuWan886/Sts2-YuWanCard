# 项目设置

## 项目依赖

项目使用游戏原生 API 和自定义实现框架。

**核心依赖**：
- `Alchyr.Sts2.ModAnalyzers` — 模组注册源码生成器
- `Krafs.Publicizer` — 使游戏内部成员可访问
- `BSchneppe.StS2.PckPacker` — 构建时打包 Godot .pck
- 游戏 DLLs（`sts2.dll`、`0Harmony.dll`）从 StS2 安装目录引用

## 项目配置 (csproj 设置)

项目提供了自动化的项目配置系统，可以自动检测游戏路径。

### Sts2PathDiscovery.props

项目包含 `Sts2PathDiscovery.props` 文件，可以自动检测不同操作系统上的游戏安装路径：

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
│   ├── Core/                  # 核心框架
│   │   ├── Abstracts/         # 抽象基类
│   │   ├── Registration/      # 注册系统
│   │   ├── Utils/             # 工具类
│   │   └── Extensions/        # 扩展方法
│   └── Utils/                 # 工具类
├── others/                    # 参考资源目录
├── MainFile.cs                # 模组入口文件
├── YourMod.csproj             # 项目配置文件
├── YourMod.json               # 模组清单文件
└── AGENTS.md                  # AI 开发指南
```

## PoolAttribute 属性

项目使用 `PoolAttribute` 属性来确定自定义内容应该添加到哪个池中。

```csharp
using YuWanCard.Core.Registration;

[Pool(typeof(SharedRelicPool))]
public class MyCustomRelic : RelicModel
{
}
```

常用的池类型：
- **卡牌池**：`SharedCardPool`、`IroncladCardPool`、`SilentCardPool`、`DefectCardPool`、`RegentCardPool`、`NecrobinderCardPool`、`ColorlessCardPool`（无色卡牌）、`TokenCardPool`、`EventCardPool`、`QuestCardPool`、`StatusCardPool`、`CurseCardPool`
- **遗物池**：`SharedRelicPool`、`IroncladRelicPool`、`SilentRelicPool`、`DefectRelicPool`、`RegentRelicPool`、`NecrobinderRelicPool`、`EventRelicPool`
- **药水池**：`SharedPotionPool`、`IroncladPotionPool`、`SilentPotionPool`、`DefectPotionPool`、`RegentPotionPool`、`NecrobinderPotionPool`、`EventPotionPool`、`TokenPotionPool`

**注意**：使用卡牌池类型时需要引入命名空间 `MegaCrit.Sts2.Core.Models.CardPools`。

## ContentRegistry 注册系统

项目使用 `ContentRegistry` 类自动扫描程序集并注册带有 `[Pool]` 属性的模型：

```csharp
using YuWanCard.Core.Registration;

// 在模组初始化时调用
ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
```

**功能**：
- 自动扫描程序集中所有带有 `[Pool]` 属性的类型
- 调用 `ModHelper.AddModelToPool` 注册到对应的池
- 统计并记录注册的卡牌、遗物、药水等数量

## IYuWanContent 接口

所有自定义内容模型都应实现 `IYuWanContent` 接口，这是一个标记接口，用于标识项目内的自定义内容：

```csharp
namespace YuWanCard.Core;

public interface IYuWanContent
{
}
```

## IYuWanCharacter 接口

自定义角色应实现 `IYuWanCharacter` 接口，提供角色特有的视觉资源路径：

```csharp
namespace YuWanCard.Core;

public interface IYuWanCharacter : IYuWanContent
{
    string? CustomVisualPath => null;
    string? CustomEnergyCounterPath => null;
    string? CustomCharacterSelectIconPath => null;
    string? CustomIconPath => null;
    string? CustomIconTexturePath => null;
    string? CustomCharacterSelectBg => null;
    string? CustomMerchantAnimPath => null;
    string? CustomRestSiteAnimPath => null;
    string? CustomArmPointingTexturePath => null;
    string? CustomArmRockTexturePath => null;
    string? CustomArmPaperTexturePath => null;
    string? CustomArmScissorsTexturePath => null;

    NCreatureVisuals? CreateCustomVisuals() => null;
    CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null;
}
```

## 自动 ID 生成

所有基类都使用正则表达式自动将类名转换为 snake_case ID：

```csharp
// PigDoubtPower -> pig_doubt_power
protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
```

**ID 前缀**：
- 卡牌：自动生成（如 `pig_strike`）
- 能力：自动生成（如 `pig_doubt_power`）
- 遗物：自动生成（如 `pig_carrot`）
- 修改器：`YUWANCARD-` 前缀（如 `YUWANCARD-ENDLESS`）

## CoreGlobalUsings 全局引用

项目使用 `CoreGlobalUsings.cs` 定义全局 using 语句：

```csharp
global using MegaCrit.Sts2.Core.Context;
global using YuWanCard.Core;
global using YuWanCard.Core.Registration;
global using YuWanCard.Core.Utils;
```

这简化了代码中的命名空间引用，所有源文件自动获得这些全局引用。
