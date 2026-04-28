# 自定义修改器

## 概述

修改器（Modifier）是一种可以改变游戏运行规则的内容，类似于《杀戮尖塔 1》的进阶模式。项目提供了 `YuWanModifierModel` 基类来简化修改器的创建。

## 基类：YuWanModifierModel

```csharp
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Modifiers;

public abstract partial class YuWanModifierModel : ModifierModel
{
    protected virtual string ModifierId => "YUWANCARD-" + ...;
    protected virtual string IconBasePath => ...;
    
    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");
}
```

## ID 自动生成

修改器 ID 自动从类名生成，格式为 `YUWANCARD-{SNAKE_CASE_NAME}`：

```csharp
// EndlessModifier -> YUWANCARD-ENDLESS
// HardcoreModifier -> YUWANCARD-HARDCORE
```

## 完整示例：无尽模式

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Modifiers;

public class EndlessModifier : YuWanModifierModel
{
    [SavedProperty]
    public int YuWanCard_EndlessLoopCount { get; set; } = 0;

    [SavedProperty]
    public int YuWanCard_TotalActsCleared { get; set; } = 0;

    [SavedProperty]
    public bool YuWanCard_HasStarted { get; set; } = false;

    public int EffectiveLoopCount => Math.Max(0, YuWanCard_EndlessLoopCount);

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        if (YuWanCard_HasStarted)
        {
            return null;
        }
        return () => ActivateEndlessMode(eventModel.Owner!, eventModel.Rng);
    }

    private async Task ActivateEndlessMode(Player player, Rng rng)
    {
        MainFile.Logger.Info("Endless mode activated!");
        YuWanCard_HasStarted = true;

        if (LocalContext.IsMe(player))
        {
            await CreatureCmd.GainMaxHp(player.Creature, 10m);
        }
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
        {
            return;
        }

        if (EffectiveLoopCount <= 0)
        {
            return;
        }

        bool isBoss = combatRoom.RoomType == RoomType.Boss;
        
        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            await ApplyDifficultyScaling(creature, isBoss);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy)
        {
            return;
        }

        if (EffectiveLoopCount <= 0)
        {
            return;
        }

        var combatRoom = creature.CombatState?.RunState?.CurrentRoom as CombatRoom;
        bool isBoss = combatRoom?.RoomType == RoomType.Boss;

        await ApplyDifficultyScaling(creature, isBoss);
    }

    public override bool ShouldAllowAncient(Player player, AncientEventModel ancient)
    {
        if (ancient is Neow && EffectiveLoopCount > 0)
        {
            return false;
        }
        return true;
    }

    protected override void AfterRunCreated(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier initialized. Loop: {YuWanCard_EndlessLoopCount}");
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier loaded. Loop: {YuWanCard_EndlessLoopCount}");
    }
}
```

## 常用钩子方法

| 方法 | 说明 |
|------|------|
| `GenerateNeowOption(EventModel)` | 生成 Neow 选项 |
| `AfterRoomEntered(AbstractRoom)` | 进入房间后 |
| `AfterCreatureAddedToCombat(Creature)` | 生物加入战斗后 |
| `ShouldAllowAncient(Player, AncientEventModel)` | 是否允许先古之民 |
| `AfterRunCreated(RunState)` | 运行创建后 |
| `AfterRunLoaded(RunState)` | 运行加载后 |
| `AfterCombatVictory(CombatState)` | 战斗胜利后 |
| `AfterCombatDefeat(CombatState)` | 战斗失败后 |
| `ModifyPlayerMaxHp(decimal, Player)` | 修改玩家最大生命值 |
| `ModifyGoldGained(decimal, Player)` | 修改获得金币 |

## SavedProperty 持久化

使用 `[SavedProperty]` 标记需要保存到存档的属性：

```csharp
[SavedProperty]
public int YuWanCard_EndlessLoopCount { get; set; } = 0;

[SavedProperty]
public bool YuWanCard_HasStarted { get; set; } = false;
```

**重要**：属性命名建议使用模组前缀（如 `YuWanCard_`），避免与其他模组冲突。

## 本地化

修改器本地化文件位于 `localization/{lang}/modifiers.json`：

```json
{
  "YUWANCARD-ENDLESS.title": "无尽模式",
  "YUWANCARD-ENDLESS.description": "击败所有章节后，重新开始循环，敌人变得更强。",
  "YUWANCARD-ENDLESS.neow_title": "无尽模式",
  "YUWANCARD-ENDLESS.neow_description": "开始无尽循环，获得 [gold]10[/gold] 最大生命值。"
}
```

## 图标资源

修改器图标路径自动生成：

```
res://YuWanCard/images/modifiers/{snake_case_name}.png
```

例如 `EndlessModifier` 的图标路径为：
```
res://YuWanCard/images/modifiers/endless.png
```

## 获取修改器实例

```csharp
// 获取特定类型的修改器
var endlessModifier = YuWanModifierModel.GetModifier<EndlessModifier>();

// 检查是否启用了无尽模式
bool isEndless = EndlessModifier.IsEndlessMode(runState);
```

## 修改器注册

修改器在构造函数中自动注册到静态列表：

```csharp
protected YuWanModifierModel()
{
    _registeredModifiers.Add(this);
}
```

可以通过 `RegisteredModifiers` 属性访问所有已注册的修改器：

```csharp
var allModifiers = YuWanModifierModel.RegisteredModifiers;
```
