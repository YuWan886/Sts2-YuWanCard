# 故障排除

## 常见问题

### Q: 如何添加新的卡牌池？

继承 `YuWanCardPoolModel`：

```csharp
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Characters;

public class MyCardPool : YuWanCardPoolModel
{
    public override string? BigEnergyIconPath =>
        "res://YuWanCard/images/characters/my_energy_counter.png";
    public override string? TextEnergyIconPath =>
        "res://YuWanCard/images/characters/my_text_energy.png";
    public override Color ShaderColor => new("FFD700");
    public override bool IsShared => false;
    public override Color DeckEntryCardColor => new("FAFAD2");
    public override Color EnergyOutlineColor => new("773726");
}
```

### Q: 如何让卡牌仅在多人模式出现？

```csharp
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.MultiplayerOnly;
```

### Q: 如何让卡牌仅在单人模式出现？

```csharp
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.SingleplayerOnly;
```

### Q: 如何正确处理金币修改？

使用 `GoldModificationGuard` 避免递归调用：

```csharp
private GoldModificationGuard? _goldGuard;

private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
    () => Owner,
    amount => Math.Floor(amount * 0.5m),
    async amount => await PlayerCmd.LoseGold(amount, Owner!)
);

public override bool ShouldGainGold(decimal amount, Player player)
{
    return GoldGuard.ShouldGainGold(amount, player);
}

public override async Task AfterGoldGained(Player player)
{
    await GoldGuard.AfterGoldGained(player);
}
```

### Q: 如何检测游戏版本？

```csharp
using YuWanCard.Utils;

var version = GameVersionCompat.GameVersion;
var currentVersion = GameVersionCompat.CurrentVersion; // 当前支持版本
```

### Q: 如何添加先古之民对话？

本地化键格式：
- 首次访问：`{ModId}-{AncientId}.talk.firstvisitEver.0-0.ancient`
- 角色对话：`{ModId}-{AncientId}.talk.{CharacterId}.{index}-{line}.ancient`
- 通用对话：`{ModId}-{AncientId}.talk.ANY.{index}-{line}.ancient`

### Q: 如何使用 CommonActions？

```csharp
using YuWanCard.Core.Utils;

// 卡牌攻击
var attackCmd = CommonActions.CardAttack(this, cardPlay, hitCount: 1);
await choiceContext.RunCommand(attackCmd);

// 卡牌格挡
var blockAmount = await CommonActions.CardBlock(this, cardPlay);

// 施加能力
await CommonActions.Apply<StrengthPower>(choiceContext, target, this, 2m);
```

### Q: 如何创建自定义 DynamicVar？

继承 `DynamicVar` 类：

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public class MyCustomVar : DynamicVar
{
    public MyCustomVar(decimal baseValue) : base("MyCustomVar", baseValue) { }
}
```

### Q: 如何在生命条上显示预测效果？

重写 `GetHealthBarForecastSegments` 方法：

```csharp
using Godot;

public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
{
    if (Amount > 0)
    {
        yield return new HealthBarForecastSegment(
            Amount,
            new Color(0.5f, 0.2f, 0.8f),
            HealthBarForecastDirection.FromRight
        );
    }
}
```

### Q: 如何添加自定义卡牌标签？

```csharp
// 使用 ModCardTagRegistry
var registry = ModCardTagRegistry.For("YUWANCARD");
var myTag = registry.RegisterOwned("MY_TAG");

// 使用已有标签
WithTags(YuWanTags.FoodPig, myTag);
```

### Q: 如何实现临时能力？

```csharp
// 简单方式：一行实现
public class PigChargePower : YuWanTemporaryPowerModelWrapper<PigCharge, StrengthPower>;
```

### Q: 如何将内容注册到自定义池？

使用 `[Pool]` 属性直接关联到自定义池：

```csharp
[Pool(typeof(MyCustomCardPool))]
public class MyCard : YuWanCardModel { ... }
```

`ContentRegistry.AutoRegisterAll()` 会自动扫描并注册所有 `[Pool]` 标记的类型。

---

## 构建问题

### 构建失败：找不到游戏路径

**问题**：构建时提示找不到 Slay the Spire 2 安装路径。

**解决方案**：
1. 确保游戏已通过 Steam 安装
2. 创建 `local.props` 文件指定游戏路径：

```xml
<Project>
    <PropertyGroup>
        <Sts2Path>Path\To\SteamLibrary\steamapps\common\Slay the Spire 2</Sts2Path>
    </PropertyGroup>
</Project>
```

### 构建失败：缺少 DLL 引用

**问题**：构建时提示找不到 `sts2.dll` 或其他游戏 DLL。

**解决方案**：
1. 确保游戏已正确安装
2. 检查 `Sts2DataDir` 路径是否正确
3. 在 `local.props` 中指定数据目录：

```xml
<Sts2DataDir>$(Sts2Path)\data_sts2_windows_x86_64</Sts2DataDir>
```

### PCK 打包失败

**问题**：发布时 .pck 文件打包失败。

**解决方案**：
1. 确保已配置 `GodotPath`
2. 检查 Godot 可执行文件路径是否正确：

```xml
<GodotPath>Path\To\MegaDot_v4.5.1-stable_mono_win64.exe</GodotPath>
```

---

## 运行时问题

### 模组未加载

**问题**：游戏启动后模组未加载。

**解决方案**：
1. 检查模组是否正确放置在 `mods/YuWanCard/` 目录
2. 确保包含以下文件：
   - `YuWanCard.dll`
   - `YuWanCard.json`
   - `YuWanCard.pck`（如果有资源）
3. 查看日志文件：`%AppData%\SlayTheSpire2\logs\godot.log`

### 本地化不显示

**问题**：卡牌或能力显示为键名而非本地化文本。

**解决方案**：
1. 检查本地化文件路径：`YuWanCard/localization/{lang}/`
2. 确保本地化键格式正确（键名必须与类名 camelCase→snake_case 转换结果一致）
3. 检查 JSON 文件语法是否正确
4. Android 平台注意前缀回退问题

### 能力描述中动态变量不显示

**问题**：能力在战斗中悬浮提示时，`{MyVar}` 显示为原始文本而非数值。

**原因**：能力的 `description` 不会自动注入 DynamicVar，动态变量必须放在 `smartDescription` 中。

**解决方案**：
将动态变量从 `description` 移到 `smartDescription`：

```json
// 错误 - description 中动态变量不会被替换
{
  "YUWANCARD-MY_POWER.description": "获得 {MyVar} 点力量。"
}

// 正确 - smartDescription 中动态变量会被替换
{
  "YUWANCARD-MY_POWER.description": "获得2点力量。",
  "YUWANCARD-MY_POWER.smartDescription": "获得 {MyVar} 点[gold]力量[/gold]。"
}
```

### 能力赋予玩家后崩溃

**问题**：赋予玩家随机能力时游戏崩溃。

**解决方案**：
1. 使用 `PowerSafetyUtils.IsSafePower()` 检查能力安全性
2. 排除模组自定义能力：

```csharp
private bool IsSafePower(PowerModel power)
{
    if (power is YuWanPowerModel) return false;
    return PowerSafetyUtils.IsSafePower(power);
}
```

### SavedProperty 警告

**问题**：日志中出现 SavedProperty 命名警告。

**解决方案**：
使用模组前缀命名属性：

```csharp
// 正确
[SavedProperty]
public int YuWanCard_MyValue { get; set; }

// 会产生警告
[SavedProperty]
public int MyValue { get; set; }
```

---

## 调试技巧

### 查看日志

日志位置：`%AppData%\SlayTheSpire2\logs\godot.log`

```csharp
// 添加调试日志
MainFile.Logger.Debug($"Debug info: {value}");
MainFile.Logger.Info($"Important info: {value}");
MainFile.Logger.Warn($"Warning: {value}");
MainFile.Logger.Error($"Error: {value}");
```

### 断点调试

1. 在 Visual Studio 或 VS Code 中打开项目
2. 设置断点
3. 使用 "附加到进程" 附加到游戏进程
4. 触发断点进行调试

### 检查模型注册

```csharp
// 检查卡牌是否注册
var card = ModelDb.Card<MyCard>();
if (card == null)
{
    MainFile.Logger.Warn("Card not registered!");
}

// 检查遗物是否注册
var relic = ModelDb.Relic<MyRelic>();
```

---

## 性能问题

### 卡牌描述加载慢

**问题**：卡牌描述加载缓慢。

**解决方案**：
1. 减少描述文本中的复杂 BBCode
2. 避免在描述中使用过多占位符
3. 使用 `smartDescription` 提供简短描述

### 战斗卡顿

**问题**：战斗中出现卡顿。

**解决方案**：
1. 缓存频繁访问的数据
2. 避免在每帧执行的代码中进行复杂计算
3. 使用异步方法避免阻塞主线程

---

## 兼容性问题

### 与其他模组冲突

**问题**：与其他模组同时使用时出现问题。

**解决方案**：
1. 检查是否有相同的 ID 冲突
2. 使用唯一的模组前缀
3. 检查 Harmony 补丁是否有冲突

### 游戏更新后模组失效

**问题**：游戏更新后模组无法正常工作。

**解决方案**：
1. 检查游戏 API 是否有变化
2. 更新项目引用的游戏 DLL
3. 查看日志了解具体错误信息
