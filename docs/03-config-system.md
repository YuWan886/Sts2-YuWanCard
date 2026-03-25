# 配置系统

BaseLib 提供了一个完整的配置系统，用于管理模组的设置。

## SimpleModConfig（推荐）

`SimpleModConfig` 是一个简化配置类，可以自动从属性生成 UI：

```csharp
using BaseLib.Config;
using Godot;

internal class MyModConfig : SimpleModConfig
{
    [ConfigSection("游戏设置")]
    public static bool EnableFeature { get; set; } = true;

    [ConfigSection("难度设置")]
    public static DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

    [SliderRange(0.1, 4.0, 0.05)]
    [SliderLabelFormat("{0:0.00}x")]
    [ConfigSection("数值调整")]
    public static double DamageMultiplier { get; set; } = 1.0;

    public MyModConfig() : base() { }

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);
    }
}

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}
```

**配置特性**：

| 特性 | 用途 |
|------|------|
| `[ConfigSection("名称")]` | 标记配置分区，自动生成分区标题 |
| `[SliderRange(min, max, step)]` | 设置滑块范围和步长 |
| `[SliderLabelFormat("格式")]` | 设置滑块标签格式 |
| `[ConfigHoverTip]` | 为设置项添加悬停提示 |
| `[HoverTipsByDefault]` | 为类中所有设置项默认添加悬停提示 |
| `[ConfigHideInUI]` | 保存和加载但不生成 UI |
| `[ConfigIgnore]` | 完全忽略此属性 |
| `[ConfigTextInput(preset)]` | 为文本输入设置字符验证 |

**重要说明**：
- 配置属性必须是**静态属性**（`static`）
- 配置属性必须有 `get` 和 `set` 访问器
- 使用 `GenerateOptionsForAllProperties` 自动生成所有选项
- 使用 `AddRestoreDefaultsButton` 添加恢复默认值按钮

## 创建配置类（手动方式）

如果需要更精细的控制，可以继承 `ModConfig` 并手动创建 UI：

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

## 注册配置

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

## 配置文件路径

配置文件默认保存在以下位置：
- Windows: `%LOCALAPPDATA%\.baselib\[ModNamespace]\[ModName].cfg`
- macOS: `~/Library/[ModNamespace]\[ModName].cfg`
- Android/iOS: Godot 用户数据目录

## 配置变更事件

可以监听配置变更事件：

```csharp
var config = new MyModConfig();
config.ConfigChanged += (sender, args) => {
};
```

## 手动保存和加载

```csharp
await config.Save();
await config.Load();
config.Changed();
```

## ModConfig 基类方法

### UI 创建方法

```csharp
// 创建开关选项
var tickbox = config.MakeToggleOption(parent, property);

// 创建下拉选项
var dropdown = config.MakeDropdownOption(parent, property);

// 创建选项容器
var container = ModConfig.MakeOptionContainer(parent, name, labelText);

// 创建分隔线
var divider = ModConfig.CreateDivider();

// 创建章节标签
var sectionLabel = config.CreateSectionLabel("Section Name");
```

### 辅助方法

```csharp
// 获取标签文本（支持本地化）
string label = config.GetLabelText("EnableFeature");

// 检查是否有设置项
bool hasSettings = config.HasSettings();
```

## SavedProperty 属性

`SavedProperty` 属性用于标记需要持久化保存的属性，适用于遗物、Modifier 等需要在游戏存档中保存状态的对象：

```csharp
using MegaCrit.Sts2.Core.Saves.Runs;

public class MyRelic : CustomRelicModel
{
    [SavedProperty]
    public bool SpecialAbilityUsed { get; set; } = false;

    [SavedProperty]
    public int StackCount { get; set; } = 0;
}

public class MyModifier : ModifierModel
{
    [SavedProperty]
    public int LoopCount { get; set; } = 0;

    [SavedProperty]
    public int TotalActsCleared { get; set; } = 0;

    [SavedProperty]
    public bool HasStarted { get; set; } = false;
}
```

**重要说明**：
- 被标记的属性会在游戏保存时自动序列化
- 加载存档时会自动恢复属性值
- 属性必须是公共的，且有 `get` 和 `set` 访问器
- 适用于基本类型（int、bool、float、string 等）和可序列化的复杂类型
- **BaseLib 会自动将包含 SavedProperty 的类型注入到 `SavedPropertiesTypeCache`**，无需手动注册

**SavedProperty 命名建议**：

BaseLib 建议为 SavedProperty 属性添加前缀以提高兼容性：

```csharp
public class MyModifier : ModifierModel
{
    [SavedProperty]
    public int MyMod_LoopCount { get; set; } = 0;  // 推荐添加前缀
}
```

如果属性名较短且没有前缀，BaseLib 会在日志中输出警告信息。

## 配置 UI 组件

BaseLib 提供了完整的配置 UI 组件系统，外观现代美观：

| 组件 | 说明 |
|------|------|
| `NConfigTickbox` | 开关选项 |
| `NConfigSlider` | 滑块选项 |
| `NConfigDropdown` | 下拉选项 |
| `NConfigDropdownItem` | 下拉选项项 |
| `NConfigButton` | 配置按钮（在模组信息旁显示） |
| `NConfigOptionRow` | 配置选项行容器 |
| `NModConfigSubmenu` | 模组配置子菜单 |

### NConfigButton 配置按钮

在模组信息按钮旁创建设置按钮，点击打开模组配置子菜单：

```csharp
// 自动创建，通过 ModConfigRegistry.Register 注册后自动显示
// 按钮打开时图标旋转动画
// 通过反射访问游戏内部菜单栈
```

### NModConfigSubmenu 配置子菜单

模组配置的完整子菜单界面：

**功能特性**：
- 滚动容器支持自动禁用滚动
- 自动保存（5 秒延迟）
- 渐入动画
- 本地化标题支持

### NConfigSlider 滑块选项

**功能特性**：
- 自定义格式字符串（`{0}` 样式）
- 负数支持（通过偏移实现）
- 控制器优化：长按加速
- 右对齐标签

```csharp
[SliderRange(-10, 10, 1)]
[SliderLabelFormat("{0}")]
[ConfigSection("数值设置")]
public static int MyValue { get; set; } = 0;
```

### NConfigDropdown 下拉选项

**功能特性**：
- 继承 `NSettingsDropdown`
- 下拉列表项位置跟随滚动
- TopLevel 解决裁剪问题

### 手动创建 UI 组件

```csharp
public override void SetupConfigUI(Control optionContainer)
{
    // 创建分区标题
    CreateSectionHeader("游戏设置");

    // 创建开关选项
    var tickbox = CreateRawTickboxControl(typeof(MyModConfig).GetProperty(nameof(EnableFeature))!);
    optionContainer.AddChild(tickbox);

    // 创建滑块选项
    var slider = CreateRawSliderControl(typeof(MyModConfig).GetProperty(nameof(DamageMultiplier))!);
    optionContainer.AddChild(slider);

    // 创建下拉选项
    var dropdown = CreateRawDropdownControl(typeof(MyModConfig).GetProperty(nameof(Difficulty))!);
    optionContainer.AddChild(dropdown);
}
```

### SimpleModConfig 辅助方法

```csharp
// 创建开关选项行（包含标签和控件）
var tickboxRow = config.CreateToggleOption(property);

// 创建滑块选项行
var sliderRow = config.CreateSliderOption(property);

// 创建下拉选项行
var dropdownRow = config.CreateDropdownOption(property);

// 创建分区标题
var sectionLabel = config.CreateSectionHeader("Section Name");

// 创建自定义按钮
var buttonRow = config.CreateButton("RowLabel", "ButtonLabel", () => {
    // 按钮点击回调
});

// 添加恢复默认值按钮
config.AddRestoreDefaultsButton(optionContainer);
```

## 悬停提示系统

使用 `[ConfigHoverTip]` 和 `[HoverTipsByDefault]` 特性为配置项添加悬停提示：

```csharp
using BaseLib.Config;

[HoverTipsByDefault]  // 为所有属性默认添加悬停提示
internal class MyModConfig : SimpleModConfig
{
    // 会自动添加悬停提示（因为类上有 HoverTipsByDefault）
    public static bool EnableFeature { get; set; } = true;

    // 禁用此属性的悬停提示
    [ConfigHoverTip(false)]
    public static int SimpleValue { get; set; } = 10;
}
```

**本地化键格式**：
- 标题：`{MODID}-{PROPERTY_NAME}.hover.title`（可选）
- 描述：`{MODID}-{PROPERTY_NAME}.hover.desc`（必需）

**settings_ui.json 示例**：
```json
{
  "MYMOD-ENABLE_FEATURE.hover.title": "启用功能",
  "MYMOD-ENABLE_FEATURE.hover.desc": "启用此功能将激活模组的核心机制。"
}
```

## 文本输入验证

使用 `[ConfigTextInput]` 特性为字符串属性设置输入验证：

```csharp
using BaseLib.Config;

internal class MyModConfig : SimpleModConfig
{
    // 使用预设验证规则
    [ConfigTextInput(TextInputPreset.SafeDisplayName, MaxLength = 16)]
    public static string PlayerName { get; set; } = "Player";

    // 使用自定义正则表达式
    [ConfigTextInput(@"[A-Za-z0-9 ]*")]
    public static string CustomField { get; set; } = "";
}
```

**TextInputPreset 预设值**：

| 预设 | 说明 |
|------|------|
| `TextInputPreset.Anything` | 无验证（默认） |
| `TextInputPreset.Alphanumeric` | 仅英文字母和数字 |
| `TextInputPreset.AlphanumericWithSpaces` | 英文字母、数字和空格 |
| `TextInputPreset.SafeDisplayName` | 国际字母、数字、空格、下划线和连字符 |

**本地化占位符**：
- 可选添加：`{MODID}-{PROPERTY_NAME}.placeholder`

## 隐藏配置属性

使用 `[ConfigHideInUI]` 和 `[ConfigIgnore]` 控制属性的可见性：

```csharp
internal class MyModConfig : SimpleModConfig
{
    // 正常显示的配置项
    public static bool EnableFeature { get; set; } = true;

    // 保存和加载，但不显示在 UI 中
    [ConfigHideInUI]
    public static int TotalRunsPlayed { get; set; } = 0;

    // 完全忽略，不保存、不加载、不显示
    [ConfigIgnore]
    public static int TemporaryValue { get; set; } = 42;
}
```

## 自定义按钮

在配置界面中添加自定义按钮：

```csharp
internal class MyModConfig : SimpleModConfig
{
    public static string PlayerName { get; set; } = "Player";

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);

        // 添加分隔线
        optionContainer.AddChild(CreateDividerControl());

        // 添加自定义按钮
        var buttonRow = CreateButton("HelloWorld", "SayHello", () =>
        {
            var name = string.IsNullOrWhiteSpace(PlayerName) ? "Player" : PlayerName;
            MainFile.Logger.Info($"Hello, {name}!");
        }, addHoverTip: true);
        optionContainer.AddChild(buttonRow);

        // 添加恢复默认值按钮
        AddRestoreDefaultsButton(optionContainer);
    }
}
```

**CreateButton 参数**：
- `rowLabelKey`：行标签的本地化键
- `buttonLabelKey`：按钮标签的本地化键
- `onPressed`：按钮点击回调
- `addHoverTip`：是否添加悬停提示
