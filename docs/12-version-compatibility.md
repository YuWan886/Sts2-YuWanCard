# 版本兼容性

## 游戏版本

项目当前支持的游戏版本：**0.103.2**

### 版本检测

使用 `GameVersionCompat` 工具类检测游戏版本：

```csharp
using YuWanCard.Utils;

// 获取游戏版本
var version = GameVersionCompat.GameVersion;

// 当前版本常量
var currentVersion = GameVersionCompat.CurrentVersion; // 0.103.2

// 版本比较
if (GameVersionCompat.GameVersion >= new Version(0, 103, 2))
{
    // 使用新 API
}
```

### 版本历史

| 游戏版本 | 模组版本 | 说明 |
|----------|----------|------|
| 0.103.2 | 当前 | main 和 beta 分支统一版本 |

---

## 平台兼容性

### 支持的平台

| 平台 | 支持状态 | 注意事项 |
|------|----------|----------|
| Windows | 完全支持 | 主要开发平台 |
| Linux | 支持 | 路径使用 `/` 分隔符 |
| macOS | 支持 | 路径使用 `/` 分隔符 |
| Android | 支持 | 注意本地化前缀回退 |
| iOS | 支持 | 注意本地化前缀回退 |

### 平台检测

使用 `RuntimePlatform` 进行平台检测：

```csharp
using YuWanCard.Utils;

// 检测移动平台
if (RuntimePlatform.IsMobileLike)
{
    // Android/iOS 特殊处理
    // 例如：调整 UI 大小、禁用某些特效
}

// 检测是否支持动态代码生成
if (RuntimePlatform.SupportsDynamicCode)
{
    // 可安全使用 Reflection.Emit
    // 可安全使用 Harmony Transpiler
}
else
{
    // AOT 运行时（如 iOS）
    // 避免使用动态代码生成
}
```

### 移动端特殊处理

Android 平台存在本地化前缀丢失的问题，项目通过 `LocalizationPrefixFallbackPatch` 自动处理：

```csharp
// 在 LocTable.GetRawText 抛出 LocException 时
// 自动重试添加 YUWANCARD- 前缀的键查找
```

---

## 依赖兼容性

### 核心依赖

| 依赖 | 版本 | 说明 |
|------|------|------|
| .NET | 9.0 | 目标框架 |
| Godot | 4.5.1 | 游戏引擎 |
| Alchyr.Sts2.ModAnalyzers | 最新 | 模组注册源码生成器 |
| Krafs.Publicizer | 最新 | 内部成员访问 |
| BSchneppe.StS2.PckPacker | 最新 | PCK 打包 |

### 可选依赖

| 依赖 | 说明 |
|------|------|
| BaseLib | 配置系统兼容 |
| RitsuLib | 配置系统兼容 |

项目通过 `ModInteropProcessor` 实现与可选依赖的互操作，无需编译时引用：

```csharp
[ModInterop("BaseLib")]
public static class BaseLibConfigInterop
{
    [InteropTarget("BaseLib.Config.ModConfigRegistry", "Register")]
    public static void Register(string modId, object config) { }
}
```

---

## API 兼容性

### 卡牌 API

| API | 版本 | 说明 |
|-----|------|------|
| `YuWanCardModel` | 0.103.2+ | 卡牌基类 |
| `WithDamage()` | 0.103.2+ | 设置伤害 |
| `WithBlock()` | 0.103.2+ | 设置格挡 |
| `WithPower<>()` | 0.103.2+ | 设置能力 |
| `ITranscendenceCard` | 0.103.2+ | 超脱卡牌接口 |
| `CardMultiplayerConstraint` | 0.103.2+ | 多人游戏限制 |

### 能力 API

| API | 版本 | 说明 |
|-----|------|------|
| `YuWanPowerModel` | 0.103.2+ | 能力基类 |
| `IHealthBarForecastSource` | 0.103.2+ | 生命条预测 |
| `YuWanTemporaryPowerModel` | 0.103.2+ | 临时能力 |
| `YuWanTemporaryPowerModelWrapper` | 0.103.2+ | 临时能力包装器 |

### 遗物 API

| API | 版本 | 说明 |
|-----|------|------|
| `YuWanRelicModel` | 0.103.2+ | 遗物基类 |
| `GetUpgradeReplacement()` | 0.103.2+ | 遗物升级链 |
| `[SavedProperty]` | 0.103.2+ | 存档属性 |

### 配置 API

| API | 版本 | 说明 |
|-----|------|------|
| `FallbackSimpleModConfig` | 0.103.2+ | 配置基类 |
| `[ConfigSection]` | 0.103.2+ | 配置分区 |
| `[ConfigSlider]` | 0.103.2+ | 滑块配置 |
| `[ConfigVisibleIf]` | 0.103.2+ | 条件显示 |

---

## 升级指南

### 从旧版本升级

1. 更新游戏到最新版本
2. 更新项目引用的游戏 DLL
3. 检查 API 变更日志
4. 更新受影响的代码
5. 重新构建和测试

### 检查 API 变更

```csharp
// 使用版本检测处理 API 差异
if (GameVersionCompat.GameVersion >= new Version(0, 103, 2))
{
    // 使用新 API
}
else
{
    // 使用旧 API 或提供回退
}
```

---

## 已知问题

### 0.103.2

- **Android 本地化前缀丢失**：已通过 `LocalizationPrefixFallbackPatch` 修复
- **AOT 运行时动态代码限制**：使用 `RuntimePlatform.SupportsDynamicCode` 检测

---

## 测试矩阵

| 功能 | Windows | Linux | macOS | Android | iOS |
|------|---------|-------|-------|---------|-----|
| 卡牌系统 | 通过 | 通过 | 通过 | 通过 | 通过 |
| 能力系统 | 通过 | 通过 | 通过 | 通过 | 通过 |
| 遗物系统 | 通过 | 通过 | 通过 | 通过 | 通过 |
| 本地化 | 通过 | 通过 | 通过 | 通过* | 通过* |
| 配置系统 | 通过 | 通过 | 通过 | 通过 | 通过 |
| 多人游戏 | 通过 | 通过 | 通过 | 未测试 | 未测试 |
| Harmony 补丁 | 通过 | 通过 | 通过 | 通过** | 通过** |

*Android 需要前缀回退补丁
**AOT 平台不支持 Transpiler
