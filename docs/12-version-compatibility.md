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