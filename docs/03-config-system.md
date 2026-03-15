# 配置系统

BaseLib 提供了一个简单的配置系统，用于管理模组的设置。

## 创建配置类

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

**自定义 Modifier 的 SavedProperty 注册**：

对于自定义 Modifier，需要通过 Harmony 补丁将其类型注入到 `SavedPropertiesTypeCache`：

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Runs;

[HarmonyPatch(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.GetJsonPropertiesForType))]
public class SavedPropertiesTypeCachePatch
{
    private static bool _injected = false;

    [HarmonyPrefix]
    public static void Prefix(Type t)
    {
        if (_injected) return;
        
        if (t == typeof(MyModifier) || t.Assembly == typeof(MyModifier).Assembly)
        {
            if (!SavedPropertiesTypeCacheContains(typeof(MyModifier)))
            {
                SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyModifier));
                MainFile.Logger.Info("MyModifier injected into SavedPropertiesTypeCache");
            }
            _injected = true;
        }
    }

    private static bool SavedPropertiesTypeCacheContains(Type type)
    {
        var cache = AccessTools.Field(typeof(SavedPropertiesTypeCache), "_cache")?.GetValue(null) as System.Collections.IDictionary;
        return cache?.Contains(type) == true;
    }
}
```
