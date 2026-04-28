# 版本兼容性

## 当前版本状态

游戏 main 分支和 beta 分支已统一为 **0.103.2** 版本，API 差异已消除。

## GameVersionCompat 工具类

`GameVersionCompat` 类提供版本检测功能：

```csharp
using YuWanCard.Utils;

// 获取游戏版本
var version = GameVersionCompat.GameVersion;

// 当前版本常量
var currentVersion = GameVersionCompat.CurrentVersion; // 0.103.2
```

## 版本常量

| 常量 | 值 | 说明 |
|------|------|------|
| `CurrentVersion` | 0.103.2 | 当前支持的版本 |

## 版本检测示例

```csharp
using YuWanCard.Utils;

public class MyCard : YuWanCardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 检查版本
        if (GameVersionCompat.GameVersion >= new Version(0, 103, 2))
        {
            // 使用新 API
        }
        else
        {
            // 使用旧 API 或显示警告
            MainFile.Logger.Warn($"Unsupported game version: {GameVersionCompat.GameVersion}");
        }
    }
}
```

## 历史版本兼容性

### 0.103.2 版本

- main 分支和 beta 分支统一
- API 差异消除
- `GameVersionCompat` 类简化

### 早期版本

早期版本存在 main 和 beta 分支的 API 差异，需要使用条件编译或运行时检测来处理。

## 最佳实践

### 1. 使用版本检测

对于可能受版本影响的功能，使用版本检测：

```csharp
if (GameVersionCompat.GameVersion >= new Version(0, 103, 2))
{
    // 新版本代码
}
```

### 2. 记录版本信息

在模组初始化时记录版本信息：

```csharp
public override void Initialize()
{
    MainFile.Logger.Info($"Game version: {GameVersionCompat.GameVersion}");
    MainFile.Logger.Info($"Mod version: {ModVersion}");
}
```

### 3. 提供降级方案

对于可能不存在的新 API，提供降级方案：

```csharp
try
{
    // 尝试使用新 API
    await NewMethod();
}
catch (MissingMethodException)
{
    // 降级到旧 API
    await OldMethod();
}
```

### 4. 使用反射处理可选功能

对于可选的新功能，使用反射：

```csharp
var method = typeof(SomeClass).GetMethod("NewMethod");
if (method != null)
{
    method.Invoke(instance, null);
}
else
{
    // 降级处理
}
```

## 更新日志

### 2024-XX-XX

- 游戏版本统一为 0.103.2
- `GameVersionCompat` 类简化
- 移除不再需要的版本兼容代码

## 参考资源

- [游戏更新日志](https://store.steampowered.com/news/app/2868840)
- [BaseLib 更新日志](https://github.com/Alchyr/BaseLib-StS2)
