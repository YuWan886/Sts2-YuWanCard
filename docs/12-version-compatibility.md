# 版本兼容性

## 概述

Slay the Spire 2 目前 main 分支和 beta 分支已统一为 0.103.2 版本，API 差异已消除。BaseLib 提供了版本兼容性工具来处理历史版本的差异。

## 版本定义

| 分支 | 版本号 | 说明 |
|------|--------|------|
| 统一版本 | 0.103.2 | main 和 beta 分支已统一 |
| Beta 阈值 | 0.103.0 | 用于区分旧版本和新版本的版本号 |

## 项目内版本检测工具

### GameVersionCompat

`GameVersionCompat` 类提供游戏版本检测功能：

```csharp
using YuWanCard.Utils;

// 获取当前游戏版本
var version = GameVersionCompat.GameVersion;
```

**注意**：由于游戏版本已统一，`GameVersionCompat` 已简化为仅提供版本检测功能，不再包含 API 兼容性封装方法。

## BaseLib 兼容性工具

### BetaMainCompatibility

BaseLib 提供了 `BetaMainCompatibility` 类来处理 API 重命名和差异：

```csharp
using BaseLib.Utils;

// 自动适配不同版本的 API
var loadedMods = BetaMainCompatibility.Renamed.LoadedMods.Get();
```

### VariableReference

`VariableReference<T>` 类可以引用多个可能名称的字段/属性/方法：

```csharp
// 创建兼容性引用
public static VariableReference<SomeType> MyField = new(
    typeof(TargetClass), "OldName", "NewName"
);

// 使用
var value = MyField.Get();
```

**内置的兼容性引用**：

| 引用 | 旧版本 | 新版本 |
|------|--------|--------|
| `LoadedMods` | `LoadedMods` 字段 | `GetLoadedMods()` 方法 |
| `FontSize` | `FontSize` | `fontSize` |
| `Font` | `Font` | `font` |
| `LineSpacing` | `LineSpacing` | `lineSpacing` |

### CustomSingletonModel 兼容性

`CustomSingletonModel` 在不支持的游戏分支上会记录警告但不会崩溃：

```csharp
public class MySingletonModel : CustomSingletonModel
{
    public MySingletonModel() : base(
        receiveCombatHooks: true,
        receiveRunHooks: true
    )
    {
        // 如果当前分支不支持，会记录警告
    }
}
```

## API 差异对照表（历史参考）

> **注意**：以下 API 差异在 0.103.2 版本之前存在。当前版本已统一，所有 API 都使用新版本（>=0.103）的签名。

| API | 旧版本 (<0.103) | 新版本 (>=0.103) |
|-----|---------------|------------------|
| `ModifyEnergyGain` | ❌ 不存在 | ✅ 存在于 AbstractModel |
| `TalkCmd.Play` | `Play(line, speaker, double, VfxColor)` | `Play(line, speaker, VfxColor, VfxDuration)` |
| `MapPointTypeCounts` 构造函数 | `(Rng rng)` | `(int unknownCount, int restCount)` |
| `VfxDuration` 枚举 | ❌ 不存在 | ✅ 存在 |
| `VfxColor` 枚举 | 8 个值 | 11 个值 (新增 Orange, Swamp, DarkGray) |
| `ModManager.LoadedMods` | 字段 | 方法 `GetLoadedMods()` |
| `ThemeConstants.Label` | PascalCase 属性 | camelCase 属性 |

## 自定义版本兼容性处理

### 创建自定义兼容性引用

```csharp
using BaseLib.Utils;

public static class MyCompatibility
{
    // 为可能重命名的 API 创建引用
    public static VariableReference<SomeType> RenamedApi = new(
        typeof(TargetClass), "OldName", "NewName", "AnotherPossibleName"
    );
    
    // 使用类型元组创建引用
    public static VariableReference<SomeType> MovedApi = new(
        (typeof(OldClass), "Property"),
        (typeof(NewClass), "Property")
    );
}
```

### 条件性代码执行

```csharp
// 检查 API 是否存在
try
{
    var value = BetaMainCompatibility.Renamed.SomeReference.Get();
    // 使用新 API
}
catch (Exception)
{
    // 回退到旧 API 或跳过功能
}
```

## 最佳实践

1. **优先使用 BaseLib 提供的兼容性工具**：`BetaMainCompatibility.Renamed` 已处理常见差异
2. **创建自定义兼容性引用**：对于 BaseLib 未覆盖的 API，使用 `VariableReference<T>`
3. **优雅降级**：当 API 不存在时，提供合理的回退方案
4. **记录日志**：在兼容性处理中记录日志，便于调试

## 错误处理

所有兼容性工具都包含错误处理：

- `VariableReference` 在找不到任何引用时抛出异常
- `CustomSingletonModel` 在不支持时记录警告但继续运行
- 使用 `try-catch` 处理可能的异常

## 相关文档

- [工具类 - BetaMainCompatibility](05-utils.md#betamaincompatibility版本兼容性工具)
- [扩展功能 - CustomSingletonModel](11-extensions.md#customsingletonmodel)
