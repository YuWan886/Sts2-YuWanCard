### 项目概述

YuWanCard 是一个基于 BaseLib 框架开发的 Slay the Spire 2 模组，实现了 11 张"猪"主题的无色卡牌、1 个能力效果和 1 个先古之民遗物。

**模组信息**：
- ID: `YuWanCard`
- 版本: `v0.1.1`
- 作者: `一条鱼丸_`
- 依赖: `BaseLib`

**内容概览**：
- 11 张无色卡牌
- 1 个能力（猪疑惑）
- 1 个先古之民遗物（七咒之戒）
- 1 个 Neow 事件选项（七咒之戒 modifier）

### 环境设置

1. 克隆本仓库
2. 确保已安装 Godot 4.5.1（Megadot 版本，版本必须匹配否则 .pck 无法加载）
3. 确保已安装 .NET 9.0 或更高版本
4. 打开项目文件 `YuWanCard.csproj`
5. 配置 Steam 和游戏路径（在 `YuWanCard.csproj` 中修改 `SteamLibraryPath`）
6. 安装所需的依赖项（包括 Harmony、BaseLib 等，通过 NuGet 自动安装）
7. 使用游戏原生的 API 以及 BaseLib 进行开发

### 项目结构

```
YuWanCard/
├── .godot/                    # Godot 引擎配置目录
├── .template.config/          # 模板配置
├── .vscode/                   # VSCode 配置
├── packages/                  # NuGet 包目录
├── YuWanCard/                 # 模组资源目录
│   ├── images/
│   │   ├── card_portraits/    # 卡牌立绘 (11 张)
│   │   ├── powers/            # 能力图标
│   │   └── relics/            # 遗物图标
│   ├── localization/zhs/      # 简体中文本地化
│   │   ├── cards.json         # 卡牌本地化
│   │   ├── powers.json        # 能力本地化
│   │   └── relics.json        # 遗物本地化
│   └── mod_image.png          # 模组图标
├── YuWanCardCode/             # 模组源代码目录
│   ├── Cards/                 # 卡牌定义 (11 张)
│   │   ├── xxxx.cs            # xxx
│   │   └── YuWanCardModel.cs  # 卡牌基类
│   ├── Patches/               # Harmony 补丁
│   │   └── NeowSevenCursesPatch.cs # Neow 事件七咒之戒选项
│   ├── Powers/                # 能力定义
│   │   ├── xxxxx.cs           # xxx
│   │   └── YuWanPowerModel.cs # 能力基类
│   └── Relics/                # 遗物定义
│       ├── xxxx.cs            # 七咒之戒
│       └── YuWanRelicModel.cs # 遗物基类
├── others/                    # 参考资源目录
├── MainFile.cs                # 模组入口文件
├── YuWanCard.csproj           # 项目配置文件
├── YuWanCard.json             # 模组清单文件
├── AGENTS.md                  # AI 开发指南
└── BaseLib使用指南.md         # BaseLib 使用文档
```

### BaseLib 依赖

本项目使用 BaseLib-StS2 作为依赖，以标准化内容添加。BaseLib 为 Slay the Spire 2 模组提供实用函数和通用结构。
[开发文档](BaseLib使用指南.md)

**核心功能**：
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

### 编译与测试

- 使用 `dotnet build` 进行编译测试
- 编译后自动复制到游戏 mods 目录
- 使用 `dotnet publish` 进行完整发布（包括 .pck 文件导出）
- 游戏日志位置：`%AppData%\SlayTheSpire2\logs\godot.log`

**构建流程**：
1. `dotnet build` - 编译代码并复制 DLL 和清单文件到 mods 目录
2. `dotnet publish` - 完整发布，包括使用 Godot 导出 .pck 文件
3. 自动复制 BaseLib 依赖到 mods 目录

### 开发最佳实践

1. **代码规范**：
   - 遵循 C# 命名约定（PascalCase 用于公共成员，camelCase 用于私有成员）
   - 使用 XML 文档注释（///）为公共 API 添加说明
   - 保持代码简洁，避免不必要的注释
   - 注释应简洁明了，避免复杂的逻辑描述
   - 使用 `PoolAttribute` 标记所有自定义模型

2. **日志记录**：
   - 使用 MainFile.Logger 进行日志记录
   - Info 级别：重要操作（初始化、保存、加载）
   - Debug 级别：详细调试信息（进度计算、卡牌过滤）
   - Warn 级别：警告信息（卡牌未找到、配置缺失）
   - Error 级别：错误信息（异常捕获）

3. **本地化**：
   - 使用游戏的本地化系统
   - 本地化文件位于 `YuWanCard/localization/zhs/`
   - 卡牌本地化键格式：`{ModId}-{CardId}.title` / `.description`
   - 能力本地化键格式：`{ModId}-{PowerId}.title` / `.description` / `.smartDescription`
   - 遗物本地化键格式：`{ModId}-{RelicId}.title` / `.description` / `.flavor`

4. **美术风格**：
   - 贴近游戏原生美术风格
   - 色彩方案：棕色/金色为主
   - 界面布局、字体样式保持一致
   - 卡牌立绘尺寸：标准卡牌尺寸
   - 能力图标：64x64（小图标）和 256x256（大图标）

5. **卡牌设计**：
   - 使用 `DynamicVar` 系统处理卡牌数值（伤害、格挡、能量等）
   - 使用 `CommonActions` 简化常见游戏动作
   - 正确使用 `TargetType` 指定目标类型
   - 合理设置稀有度和费用

6. **能力设计**：
   - 继承 `CustomPowerModel` 并实现相应事件方法
   - 使用 `CustomPackedIconPath` 指定图标路径
   - 正确设置 `PowerType` 和 `StackType`
   - 实现事件触发方法（如 `AfterSideTurnStart`）

7. **遗物设计**：
   - 继承 `CustomRelicModel` 并实现相应钩子方法
   - 使用 `PoolAttribute` 标记遗物池
   - 正确设置 `RelicRarity`
   - 常用钩子方法：
     - `AfterObtained()`：获得遗物时触发
     - `ModifyDamageMultiplicative()`：修改伤害倍率
     - `ModifyBlockMultiplicative()`：修改格挡倍率
     - `ModifyMaxEnergy()`：修改最大能量
     - `ModifyHandDraw()`：修改抽牌数
     - `ModifyRestSiteHealAmount()`：修改休息处回复血量
     - `ShouldAllowSelectingMoreCardRewards()`：允许选择更多卡牌奖励

8. **Harmony 补丁**：
   - 用于修改游戏原有行为（如添加 Neow 事件选项）
   - 使用 `[HarmonyPatch]` 标记补丁类
   - 使用 `[HarmonyPostfix]`、`[HarmonyPrefix]` 等标记补丁方法
   - 通过反射调用私有方法（如 `AncientEventModel.Done()`）

### 参考资源

- 游戏源代码：[others/sts2-src/](others/sts2-src/)
- BaseLib 项目：[others/BaseLib-StS2-master](others/BaseLib-StS2-master)
- BaseLib 使用指南：[BaseLib使用指南.md](BaseLib使用指南.md)
- STS2 Mod 文档：https://github.com/Cany0udance/EarlySts2ModdingGuides/wiki
- MCP 工具：context7（文档查询）、github（代码搜索）
- 智能体：Search（代码库搜索）
