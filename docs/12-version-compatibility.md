# 版本兼容性

## 概述

Slay the Spire 2 有两个主要分支：main 分支（稳定版）和 beta 分支（测试版）。这两个分支之间存在 API 差异，BaseLib 提供了多种工具来处理这些差异。

## 版本定义

| 分支 | 版本号 | 说明 |
|------|--------|------|
| Main | 0.99.1 | 稳定发布版本 |
| Beta | 0.103.0 | 测试分支版本 |

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

| 引用 | Main 分支 | Beta 分支 |
|------|-----------|-----------|
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

## API 差异对照表

| API | Main (0.99.1) | Beta (0.103.0) |
|-----|---------------|----------------|
| `ModifyEnergyGain` | ❌ 不存在 | ✅ 存在于 AbstractModel |
| `TalkCmd.Play` | `Play(line, speaker, double, VfxColor)` | `Play(line, speaker, VfxColor, VfxDuration)` |
| `MapPointTypeCounts` 构造函数 | `(Rng rng)` | `(int unknownCount, int restCount)` |
| `VfxDuration` 枚举 | ❌ 不存在 | ✅ 存在 |
| `VfxColor` 枚举 | 8个值 | 11个值 (新增 Orange, Swamp, DarkGray) |
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
