# 配置系统

## 配置系统

项目使用配置系统管理模组设置，支持自动生成配置 UI 和持久化存储。

## SimpleModConfig 基类

所有配置类应继承 `SimpleModConfig` 基类：

```csharp
namespace YuWanCard.Config;

public class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool EnableDeathEffect { get; set; } = true;

    [ConfigSection("多人游戏设置")]
    [ConfigHoverTip]
    public static bool BypassModelDbHashCheck { get; set; } = false;

    public YuWanCardConfig() : base() { }
}
```

## 配置特性

### ConfigSection

标记配置分区，自动生成分区标题：

```csharp
[ConfigSection("显示设置")]
public static bool EnableDeathEffect { get; set; } = true;
```

### ConfigHoverTip

为设置项添加悬停提示，自动从本地化文件读取描述：

```csharp
[ConfigHoverTip]
public static bool EnableDeathEffect { get; set; } = true;
```

### ConfigSlider

设置滑块范围、步长和标签格式：

```csharp
[ConfigSlider(0, 100, 5, "{0}%")]
public static int Volume { get; set; } = 50;
```

### ConfigHideInUI

保存和加载但不生成 UI：

```csharp
[ConfigHideInUI]
public static string InternalValue { get; set; } = "";
```

### ConfigIgnore

完全忽略此属性：

```csharp
[ConfigIgnore]
public static string DebugValue { get; set; } = "";
```

### ConfigTextInput

为文本输入设置字符验证：

```csharp
[ConfigTextInput(TextInputPreset.Url)]
public static string ServerUrl { get; set; } = "http://localhost";
```

### ConfigVisibleIf

条件显示配置项：

```csharp
[ConfigVisibleIf(nameof(EnableAutoSlay), "true")]
public static int AutoSlayDelay { get; set; } = 1000;
```

## 配置属性要求

**重要规则**：
1. 配置属性必须是 **静态属性**（`static`）
2. 配置属性必须有 `get` 和 `set` 访问器
3. 配置属性应有默认值

```csharp
// 正确示例
[ConfigSection("游戏设置")]
[ConfigHoverTip]
public static int MaxCards { get; set; } = 10;

// 错误示例 - 非静态
[ConfigSection("游戏设置")]
public int MaxCards { get; set; } = 10;  // 编译错误

// 错误示例 - 缺少 set 访问器
[ConfigSection("游戏设置")]
public static int MaxCards { get; }  // 无法保存
```

## 完整配置示例

```csharp
namespace YuWanCard.Config;

public class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool EnableDeathEffect { get; set; } = true;

    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    [ConfigSlider(0, 100, 10, "{0}%")]
    public static int EffectIntensity { get; set; } = 50;

    [ConfigSection("多人游戏设置")]
    [ConfigHoverTip]
    public static bool BypassModelDbHashCheck { get; set; } = false;

    [ConfigSection("更新设置")]
    [ConfigHoverTip]
    public static bool EnableAutoUpdateCheck { get; set; } = true;

    [ConfigSection("自动爬塔设置")]
    [ConfigHoverTip]
    public static bool EnableAutoSlay { get; set; } = false;

    [ConfigSection("自动爬塔设置")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(EnableAutoSlay), "true")]
    [ConfigSlider(100, 5000, 100, "{0}ms")]
    public static int AutoSlayDelay { get; set; } = 1000;

    public YuWanCardConfig() : base() { }
}
```

## SavedProperty 属性

用于持久化保存属性（与配置不同，这些会保存到存档中）：

```csharp
using MegaCrit.Sts2.Core.Saves.Runs;

public class MyRelic : RelicModel
{
    [SavedProperty]
    public int YuWanCard_EndlessLoopCount { get; set; } = 0;

    [SavedProperty]
    public bool YuWanCard_HasStarted { get; set; } = false;
}
```

**重要**：
- 属性命名建议使用模组前缀（如 `YuWanCard_`），否则会产生警告
- `SavedProperty` 用于存档数据，`SimpleModConfig` 用于模组设置

## 配置 UI 生成

配置系统会自动为配置类生成 UI：

1. `[ConfigSection]` 创建分区标题
2. 根据属性类型自动选择控件：
   - `bool` → 复选框
   - `int`/`decimal` → 滑块（使用 `[ConfigSlider]`）
   - `string` → 文本框（使用 `[ConfigTextInput]`）
3. `[ConfigHoverTip]` 添加悬停提示

## 本地化

配置的悬停提示从本地化文件读取，键格式为：

```
{config_key}.description
```

例如：
```json
{
  "EnableDeathEffect.description": "启用死亡特效",
  "BypassModelDbHashCheck.description": "绕过模型数据库哈希检查"
}
```

## 访问配置值

```csharp
// 直接访问静态属性
if (YuWanCardConfig.EnableDeathEffect)
{
    // 执行死亡特效
}

// 获取配置值
var volume = YuWanCardConfig.Volume;
```

## 配置文件存储位置

配置文件存储在游戏存档目录：

```
%SlayTheSpire2%\saves\mods\{ModId}\config.json
```
