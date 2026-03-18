# 最佳实践

## 命名约定

- 类名：使用 PascalCase
- 方法名：使用 PascalCase
- 属性名：使用 PascalCase
- 字段名：使用 camelCase 或 _camelCase
- 命名空间：使用 PascalCase，通常以模组名称开头

## 组织代码

- 将不同类型的内容放在不同的文件夹中
- 使用命名空间来组织代码
- 保持代码简洁明了
- 使用部分类（partial classes）来组织大型类

## 调试

使用 BaseLib 的日志系统：

```csharp
using BaseLib;

MainFile.Logger.Info("Mod initialized");
MainFile.Logger.Warn("Something might be wrong");
MainFile.Logger.Error("An error occurred");
MainFile.Logger.Debug("Detailed debug information");
```

**日志级别**：
- `Info`：重要操作（初始化、保存、加载）
- `Debug`：详细调试信息（进度计算、卡牌过滤）
- `Warn`：警告信息（卡牌未找到、配置缺失）
- `Error`：错误信息（异常捕获）

**游戏日志位置**：
- Windows: `C:\Users\[用户名]\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs/godot.log`

## 性能

- 避免在游戏循环中做 heavy 操作
- 使用对象池来减少 GC
- 合理使用 Harmony 补丁
- 使用缓存来减少重复计算
- 延迟加载资源和初始化

## 代码规范

- 使用 XML 文档注释（///）为公共 API 添加说明
- 遵循 C# 编码规范
- 保持方法简洁，每个方法只做一件事
- 使用有意义的变量和方法名
- 注释应简洁明了，避免复杂的逻辑描述

## 本地化

- 使用游戏的本地化系统
- 为所有用户可见的文本提供本地化支持
- 遵循游戏的本地化命名约定

**本地化文件格式**：
```json
{
  "MODID-CARD_ID.title": "卡牌名称",
  "MODID-CARD_ID.description": "卡牌描述，支持 {DynamicVar:diff()} 等动态变量",
  "MODID-POWER_ID.title": "能力名称",
  "MODID-POWER_ID.description": "能力描述",
  "MODID-POWER_ID.smartDescription": "能力智能描述"
}
```

**本地化键命名规则**：
- 卡牌：`{ModId}-{CardId}.title` / `.description`
- 能力：`{ModId}-{PowerId}.title` / `.description` / `.smartDescription`
- 遗物：`{ModId}-{RelicId}.title` / `.description` / `.flavor`
- 先古之民：`{ModId}-{AncientId}.title` / `.epithet` / `.pages.{PageName}.description`
- Modifier：`{ModifierId}.title` / `.description` / `.neow_title` / `.neow_description`
- ModId 和 CardId/PowerId 使用大写，用连字符分隔

**描述中的动态变量**：
- `{Damage:diff()}` - 显示伤害值
- `{Block:diff()}` - 显示格挡值
- `{Heal:diff()}` - 显示治疗值
- `{Energy:diff()}` - 显示能量值
- `{Energy:energyIcons()}` - 显示能量图标
- `{PowerName:diff()}` - 显示能力层数
- `{IfUpgraded:show:升级后文本|升级前文本}` - 根据是否升级显示不同文本

**颜色标签**：
- `[gold]文本[/gold]` - 金色文本
- `[red]文本[/red]` - 红色文本
- `[blue]文本[/blue]` - 蓝色文本

## 安全性

- 使用 IL 分析避免赋予怪物专属能力
- 检查 `Owner` 是否为 null
- 使用 `LocalContext.IsMe(player)` 检查是否为本地玩家
- 避免在多人游戏中执行仅限本地的操作

## 多人游戏

Slay the Spire 2 支持多人合作模式，模组开发时需要特别注意多人同步问题。

### CardMultiplayerConstraint

使用 `CardMultiplayerConstraint` 标记卡牌的多人游戏限制：

```csharp
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

[Pool(typeof(ColorlessCardPool))]
public class MyMultiplayerCard : CustomCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public MyMultiplayerCard() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly
    )
    {
    }
}
```

**CardMultiplayerConstraint 枚举值**：
- `CardMultiplayerConstraint.None`：无限制（默认值）
- `CardMultiplayerConstraint.MultiplayerOnly`：仅限多人游戏，单人模式不会出现

### LocalContext.IsMe

使用 `LocalContext.IsMe(player)` 检查是否为本地玩家，避免在多人游戏中执行重复操作：

```csharp
using MegaCrit.Sts2.Core.Context;

public override async Task AfterObtained()
{
    await base.AfterObtained();

    if (LocalContext.IsMe(Owner))
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, 10m);
    }
}
```

**重要说明**：
- 在 Modifier 的 `GenerateNeowOption` 中，效果会对所有玩家触发
- 使用 `LocalContext.IsMe` 可以确保效果只对本地玩家执行一次
- 对于全局效果（如敌人增强），不需要检查 `IsMe`

### 获取队友

使用 `CombatState.GetTeammatesOf(creature)` 获取队友列表：

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;

public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    IEnumerable<Creature> teammates = from c in CombatState!.GetTeammatesOf(Owner.Creature)
                                      where c != null && c.IsAlive && c.IsPlayer
                                      select c;

    foreach (Creature teammate in teammates)
    {
        await PlayerCmd.GainEnergy(1, teammate.Player!);
    }
}
```

**注意**：
- `GetTeammatesOf` 返回包括自己在内的所有队友
- 使用 `where c != Owner.Creature` 排除自己
- 检查 `c.IsAlive` 确保队友存活

### 目标类型

多人游戏相关的 `TargetType`：

| TargetType | 说明 |
|------------|------|
| `TargetType.Self` | 自身 |
| `TargetType.AllAllies` | 所有队友（包括自己） |
| `TargetType.AnyAlly` | 任意队友（包括自己） |
| `TargetType.AnyPlayer` | 任意玩家（可用于选择死亡玩家） |

### 玩家选择同步

在多人游戏中，当需要玩家做出选择时，需要使用 `PlayerChoiceSynchronizer` 进行同步：

```csharp
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Context;

private async Task<Creature?> SelectDeadPlayer(PlayerChoiceContext choiceContext, List<Creature> deadPlayers)
{
    uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(Owner);
    await choiceContext.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

    int selectedIndex;
    if (LocalContext.IsMe(Owner))
    {
        selectedIndex = await ShowDeadPlayerSelection(deadPlayers);
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
            Owner,
            choiceId,
            PlayerChoiceResult.FromIndex(selectedIndex)
        );
    }
    else
    {
        selectedIndex = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(Owner, choiceId)).AsIndex();
    }

    await choiceContext.SignalPlayerChoiceEnded();

    if (selectedIndex < 0 || selectedIndex >= deadPlayers.Count)
    {
        return null;
    }

    return deadPlayers[selectedIndex];
}
```

**同步流程**：
1. 使用 `ReserveChoiceId` 预留选择 ID
2. 使用 `SignalPlayerChoiceBegun` 通知选择开始
3. 本地玩家：执行选择逻辑，使用 `SyncLocalChoice` 同步结果
4. 远程玩家：使用 `WaitForRemoteChoice` 等待远程选择结果
5. 使用 `SignalPlayerChoiceEnded` 通知选择结束

### CardMultiplayerConstraint 属性

在获取卡牌池时，需要传入 `CardMultiplayerConstraint` 参数：

```csharp
var availableCards = ModelDb.CardPool<ColorlessCardPool>()
    .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint);
```

这确保了：
- 单人模式：不返回 `MultiplayerOnly` 的卡牌
- 多人模式：返回所有卡牌

### 多人卡牌设计建议

1. **仅限多人的卡牌**：使用 `CardMultiplayerConstraint.MultiplayerOnly`
2. **影响队友的卡牌**：使用 `TargetType.AllAllies` 或 `TargetType.AnyAlly`
3. **需要玩家选择的卡牌**：使用 `PlayerChoiceSynchronizer` 同步
4. **效果只执行一次**：使用 `LocalContext.IsMe` 检查
5. **全局效果**：不需要检查 `IsMe`，会自动同步

## 模组互操作 (ModInterop)

### 创建软依赖

使用 `ModInterop` 创建对其他模组的软依赖：

```csharp
using BaseLib.Utils.ModInterop;

[ModInterop("OtherModId")]
public class OtherModInterop : InteropClassWrapper
{
    [InteropTarget]
    public static Type? OtherModCardType { get; set; }

    public static CardModel? GetOtherModCard()
    {
        if (OtherModCardType == null) return null;
        return Activator.CreateInstance(OtherModCardType) as CardModel;
    }
}
```

### 检查依赖状态

```csharp
// 检查目标模组是否加载
if (OtherModInterop.IsLoaded)
{
    var card = OtherModInterop.GetOtherModCard();
    // 使用 card...
}
```

### 最佳实践

1. **始终检查 null**：使用互操作目标前检查是否为 null
2. **使用 IsLoaded**：检查目标模组是否已加载
3. **提供降级方案**：目标模组不存在时提供替代功能
4. **文档说明**：在模组说明中标注可选依赖

## IL 补丁最佳实践

### 使用 InstructionPatcher

推荐使用 BaseLib 提供的 `InstructionPatcher` 简化 Transpiler：

```csharp
using BaseLib.Utils.Patching;
using HarmonyLib;

[HarmonyTranspiler]
public static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions)
{
    var patcher = new InstructionPatcher(instructions);

    while (patcher.Find(new IMatcher[]
    {
        InstructionMatcher.OpCode(OpCodes.Call, oldMethod)
    }))
    {
        patcher.GetLabels(out var labels);
        patcher.Replace(new CodeInstruction(OpCodes.Call, newMethod).WithLabels(labels));
    }

    return patcher;
}
```

### 保留标签

修改指令时务必保留标签，否则可能导致跳转错误：

```csharp
patcher.GetLabels(out var labels);
patcher.Replace(new CodeInstruction(...).WithLabels(labels));
```

### 调试 IL 补丁

1. **使用日志输出指令**：
```csharp
foreach (var instr in instructions)
{
    MainFile.Logger.Debug($"{instr.opcode} {instr.operand}");
}
```

2. **使用 dnSpy 或 ILSpy**：查看原始方法的 IL 代码

3. **逐步调试**：先匹配再修改，确认位置正确

### 常见问题

1. **标签丢失**：使用 `GetLabels` 保留标签
2. **匹配失败**：检查操作码和操作数是否正确
3. **堆栈不平衡**：确保指令序列的堆栈操作正确
