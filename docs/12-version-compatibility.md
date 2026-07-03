# 版本兼容性

## 游戏版本

项目当前支持的游戏版本：**0.108.0**

### 版本检测

使用 `GameVersionCompat` 工具类检测游戏版本：

```csharp
using YuWanCard.Utils;

// 获取游戏版本
var version = GameVersionCompat.GameVersion;

// 当前版本常量
var currentVersion = GameVersionCompat.CurrentVersion; // 0.107.1

// 版本比较
if (GameVersionCompat.GameVersion >= new Version(0, 107, 1))
{
    // 使用新 API
}
```

### 版本历史

| 游戏版本 | 模组版本 | 说明 |
|----------|----------|------|
| 0.107.1 | v0.5.7 | 当前版本 |

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

游戏本身会自动处理平台差异。对于 Android 平台，`LocalizationPrefixFallbackPatch` 自动处理本地化前缀丢失问题。

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
| RitsuLib | 配置系统兼容 |

配置页面通过运行时反射注册到 `STS2RitsuLib`，无需编译时引用：

```csharp
internal static class ConfigRegistrar
{
    public static void TryDeferredRegister() { }
}
```
