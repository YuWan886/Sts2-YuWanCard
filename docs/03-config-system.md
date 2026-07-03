# 配置系统

## 概览

YuWanCard 的配置现在只保留 **STS2-RitsuLib** 集成。

- `YuWanCardCode/Config/YuWanCardConfig.cs` 只负责保存静态配置值和默认值
- `YuWanCardCode/Config/ConfigRegistrar.cs` 负责在运行时反射生成 RitsuLib settings provider
- 配置 UI 的页面、分组、排序、本地化键和数据键都集中写在 `ConfigRegistrar.cs`

## 当前模式

```csharp
namespace YuWanCard.Config;

public class YuWanCardConfig
{
    public static bool EnableDeathEffect { get; set; } = true;
    public static double CursorScale { get; set; } = 2.0;
}
```

规则：

1. 配置项必须是 `public static` 且同时有 `get` / `set`
2. 配置项必须提供默认值
3. 新增配置时，要同时更新 `YuWanCardConfig` 和 `ConfigRegistrar`

## RitsuLib 注册

`ConfigRegistrar.TryDeferredRegister()` 会在运行时检测 `STS2RitsuLib`，然后：

1. 读取 `RitsuPages` / `RitsuSections`
2. 为 toggle、slider、subpage、custom entry 动态发射 provider 类型
3. 调用 `RitsuLibFramework.RegisterModSettingsReflectionProviderAndTryRegister`
4. 通过 `RitsuConfigRuntimeBridge` 将 UI 值回写到 `YuWanCardConfig`

`RitsuConfigRuntimeBridge` 还负责少量即时副作用，例如刷新自定义鼠标。

## 新增配置项时要改哪里

布尔配置：

1. 在 `YuWanCardConfig` 增加静态属性
2. 在 `ConfigRegistrar.ToggleProps` 增加一项
3. 如需分组或分页，确认 `RitsuSections` / `RitsuPages` 已覆盖
4. 在 `YuWanCard/localization/*/settings_ui.json` 补齐标题/描述文案

数值滑块：

1. 在 `YuWanCardConfig` 增加 `double` 静态属性
2. 在 `ConfigRegistrar.SliderProps` 增加一项
3. 填好 `Min`、`Max`、`Step`、`Format`

## SavedProperty 与配置的区别

- `SavedProperty`：跟随存档，属于 run 内状态
- `YuWanCardConfig`：全局模组设置，属于玩家本地配置

不要把局内状态塞进 `YuWanCardConfig`，也不要把用户偏好写成 `SavedProperty`。
