# 版本兼容性

## 概述

`GameVersionCompat` 是一个游戏版本兼容性工具类，用于处理 Slay the Spire 2 不同版本（main 分支和 beta 分支）之间的 API 差异。它提供统一的接口，让模组开发者无需关注版本差异即可使用统一 API。

## 版本定义

| 分支 | 版本号 | 说明 |
|------|--------|------|
| Main | 0.99.1 | 稳定发布版本 |
| Beta | 0.102.0 | 测试分支版本 |

## API 差异对照表

| API | Main (0.99.1) | Beta (0.102.0) |
|-----|---------------|----------------|
| `ModifyEnergyGain` | ❌ 不存在 | ✅ 存在于 AbstractModel |
| `TalkCmd.Play` | `Play(line, speaker, double, VfxColor)` | `Play(line, speaker, VfxColor, VfxDuration)` |
| `MapPointTypeCounts` 构造函数 | `(Rng rng)` | `(int unknownCount, int restCount)` |
| `VfxDuration` 枚举 | ❌ 不存在 | ✅ 存在 |
| `VfxColor` 枚举 | 8个值 | 11个值 (新增 Orange, Swamp, DarkGray) |

## 版本检测

```csharp
using YuWanCard.Utils;

// 获取当前游戏版本
Version? version = GameVersionCompat.GameVersion;

// 检测分支
bool isMain = GameVersionCompat.IsMainBranch;  // < 0.102.0
bool isBeta = GameVersionCompat.IsBetaBranch;  // >= 0.102.0
string branch = GameVersionCompat.BranchName;  // "main" 或 "beta"

// API 能力检测
bool hasModifyEnergyGain = GameVersionCompat.HasModifyEnergyGainHook;
bool hasVfxDuration = GameVersionCompat.HasVfxDurationEnum;
```

## 统一 API 接口

### TalkCmdPlay - 对话气泡

```csharp
// 统一接口，自动适配不同版本
GameVersionCompat.TalkCmdPlay(line, creature, VfxColor.Red, 3.0);

// Main 分支实现
// TalkCmd.Play(line, speaker, 3.0, VfxColor.Red)

// Beta 分支实现
// TalkCmd.Play(line, speaker, VfxColor.Red, VfxDuration.Custom)
```

### CreateMapPointTypeCounts - 地图点类型计数

```csharp
// 统一接口，自动适配不同版本
var counts = GameVersionCompat.CreateMapPointTypeCounts(rng, unknownCount: 12, restCount: 5);

// Main 分支实现
// new MapPointTypeCounts(rng)

// Beta 分支实现
// new MapPointTypeCounts(12, 5)
```

### TrySetNumOfElites - 设置精英数量

```csharp
// 统一接口，通过反射设置属性
bool success = GameVersionCompat.TrySetNumOfElites(mapPointTypeCounts, newEliteCount);
```

## 能量翻倍实现示例

```csharp
public class RainDarkPower : YuWanPowerModel
{
    // Beta 分支：使用 ModifyEnergyGain hook
    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (GameVersionCompat.HasModifyEnergyGainHook && player == Owner.Player && amount > 0)
        {
            return amount * 2;
        }
        return amount;
    }

    // Main 分支：使用 AfterEnergyReset
    public override async Task AfterEnergyReset(Player player)
    {
        if (GameVersionCompat.ShouldUseAfterEnergyReset && player == Owner.Player)
        {
            int currentEnergy = Owner.Player?.PlayerCombatState?.Energy ?? 0;
            if (currentEnergy > 0)
            {
                await PlayerCmd.GainEnergy(currentEnergy, Owner.Player);
            }
        }
    }
}
```

## 初始化

建议在模组入口处调用初始化方法，以便提前检测和缓存版本信息：

```csharp
public override void _Ready()
{
    GameVersionCompat.Initialize();
    // 其他初始化代码...
}
```

## 错误处理

所有统一 API 接口都包含错误处理：

- 版本检测失败时返回合理的默认值
- API 调用失败时记录错误日志
- 使用 `MainFile.Logger` 记录详细日志信息

## 架构设计

### 版本检测机制

```
GameVersionCompat
├── 版本常量
│   ├── MainBranchVersion (0.99.1)
│   └── BetaBranchVersion (0.102.0)
├── 版本属性
│   ├── GameVersion      // 从 ReleaseInfoManager 获取
│   ├── IsMainBranch     // < BetaBranchVersion
│   ├── IsBetaBranch     // >= BetaBranchVersion
│   └── BranchName       // "main" 或 "beta"
└── API 能力检测
    ├── HasModifyEnergyGainHook
    ├── HasVfxDurationEnum
    └── MapPointTypeCounts 构造函数检测
```

### API 调用路由

```
统一 API 调用
    │
    ├── 检测当前版本
    │       │
    │       ├── IsBetaBranch → 使用 Beta 分支实现
    │       │
    │       └── IsMainBranch → 使用 Main 分支实现
    │
    └── 返回统一结果
```

### 错误处理流程

```
API 调用
    │
    ├── 成功 → 返回结果
    │
    └── 失败
            │
            ├── 记录错误日志 (MainFile.Logger.Error)
            │
            └── 返回默认值或 null
```
