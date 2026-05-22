# 自定义修改器

## 概述

修改器（Modifier）用于改变游戏的基本规则，可以添加新的游戏机制或改变现有机制。

## 基类：YuWanModifierModel

项目使用 `YuWanModifierModel` 作为所有修改器的基类：

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
    public bool YuWanCard_HasStarted { get; set; } = false;

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        if (YuWanCard_HasStarted) return null;
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
        if (room is not CombatRoom combatRoom || YuWanCard_EndlessLoopCount <= 0)
            return;

        bool isBoss = combatRoom.RoomType == RoomType.Boss;
        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            await ApplyDifficultyScaling(creature, isBoss);
        }
    }

    private async Task ApplyDifficultyScaling(Creature creature, bool isBoss)
    {
        float hpMultiplier = 1f + (0.35f * YuWanCard_EndlessLoopCount);
        if (isBoss) hpMultiplier += 0.20f * YuWanCard_EndlessLoopCount;

        int newMaxHp = (int)(creature.MaxHp * hpMultiplier);
        await CreatureCmd.SetMaxHp(creature, newMaxHp);
        await CreatureCmd.Heal(creature, newMaxHp - creature.CurrentHp, playAnim: false);

        int strengthBonus = 2 * YuWanCard_EndlessLoopCount;
        if (strengthBonus > 0)
        {
            await PowerCmd.Apply<StrengthPower>(creature, strengthBonus, null, null);
        }
    }
}
```

## 修改器 ID

修改器 ID 通过 `ModifierId` 属性自动生成。基类会自动：
1. 去掉类名末尾的 `Modifier` 后缀
2. 将驼峰命名转换为大写下划线格式
3. 添加 `YUWANCARD-` 前缀

例如：`EndlessModifier` → `YUWANCARD-ENDLESS`

也可以手动覆盖 `ModifierId` 属性来指定自定义 ID。

## 修改器图标

图标路径通过 `IconBasePath` 自动生成（去掉 `Modifier` 后缀后转为小写下划线格式）：

```
images/modifiers/{snake_case_name}.png
```

例如 `EndlessModifier` → `images/modifiers/endless.png`

## 修改器本地化

修改器本地化键格式：

```
YUWANCARD-{ModifierId}.title
YUWANCARD-{ModifierId}.description
YUWANCARD-{ModifierId}.neow_title
YUWANCARD-{ModifierId}.neow_description
```

## 常用钩子方法

| 方法 | 说明 |
|------|------|
| `GenerateNeowOption(EventModel)` | 生成 Neow 选项 |
| `AfterRoomEntered(AbstractRoom)` | 进入房间后 |
| `AfterCombatVictory()` | 战斗胜利后 |
| `AfterCombatDefeat()` | 战斗失败后 |
| `ModifyDamage(decimal, DamageInfo, Creature)` | 修改伤害 |
| `ModifyBlock(decimal, Creature)` | 修改格挡 |

## 每日挑战控制

重写 `AllowedInDailyRun` 控制修改器是否可被每日挑战随机选择：

```csharp
public override bool AllowedInDailyRun => false;  // 默认值，大多数修改器不应进入每日挑战
```

## 获取已注册修改器

```csharp
// 获取特定修改器实例
var endless = YuWanModifierModel.GetModifier<EndlessModifier>();

// 获取所有已注册修改器
var allModifiers = YuWanModifierModel.RegisteredModifiers;
```

修改器在构造函数中自动注册到静态列表 `RegisteredModifiers`。

## 存档属性

使用 `[SavedProperty]` 标记需要持久化的属性：

```csharp
[SavedProperty]
public int YuWanCard_EndlessLoopCount { get; set; } = 0;
```

**重要**：属性命名建议使用模组前缀（如 `YuWanCard_`），否则会产生警告。

## 完整示例

### 无尽模式修改器

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
    public bool YuWanCard_HasStarted { get; set; } = false;

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        if (YuWanCard_HasStarted) return null;
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
        if (room is not CombatRoom combatRoom || YuWanCard_EndlessLoopCount <= 0)
            return;

        bool isBoss = combatRoom.RoomType == RoomType.Boss;
        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            await ApplyDifficultyScaling(creature, isBoss);
        }
    }

    private async Task ApplyDifficultyScaling(Creature creature, bool isBoss)
    {
        float hpMultiplier = 1f + (0.35f * YuWanCard_EndlessLoopCount);
        if (isBoss) hpMultiplier += 0.20f * YuWanCard_EndlessLoopCount;

        int newMaxHp = (int)(creature.MaxHp * hpMultiplier);
        await CreatureCmd.SetMaxHp(creature, newMaxHp);
        await CreatureCmd.Heal(creature, newMaxHp - creature.CurrentHp, playAnim: false);

        int strengthBonus = 2 * YuWanCard_EndlessLoopCount;
        if (strengthBonus > 0)
        {
            await PowerCmd.Apply<StrengthPower>(creature, strengthBonus, null, null);
        }
    }
}
```

### 本地化文件 (modifiers.json)

```json
{
  "YUWANCARD-ENDLESS.title": "无尽模式",
  "YUWANCARD-ENDLESS.description": "击败所有章节后重新开始，敌人变得更强。",
  "YUWANCARD-ENDLESS.neow_title": "无尽模式",
  "YUWANCARD-ENDLESS.neow_description": "开始无尽循环，获得 [gold]10[/gold] 点最大生命值。"
}
```

## 注意事项

1. **存档属性命名**：使用模组前缀避免冲突
2. **玩家身份检查**：在多人游戏中使用 `LocalContext.IsMe(player)` 检查
3. **日志记录**：使用 `MainFile.Logger` 记录重要操作
4. **异步方法**：所有钩子方法都是异步的，使用 `await` 等待操作完成
