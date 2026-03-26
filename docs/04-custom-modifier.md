# 自定义 Modifier

Modifier（修改器）是一种可以修改游戏规则的特殊模型，用于创建自定义游戏模式（如无尽模式）。

## 创建自定义 Modifier

继承 `ModifierModel` 来创建自定义修改器：

```csharp
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves.Runs;

public class EndlessModifier : ModifierModel
{
    public const string ModifierId = "YUWANCARD-ENDLESS";

    [SavedProperty]
    public int YuWanCard_EndlessLoopCount { get; set; } = 0;

    [SavedProperty]
    public int YuWanCard_TotalActsCleared { get; set; } = 0;

    [SavedProperty]
    public bool YuWanCard_HasStarted { get; set; } = false;

    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");

    public override string IconPath => "res://YuWanCard/images/modifiers/endless.png";

    public int EffectiveLoopCount => Math.Max(0, YuWanCard_EndlessLoopCount);

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        if (YuWanCard_HasStarted)
        {
            return null;
        }
        return () => ActivateModifier(eventModel.Owner!, eventModel.Rng);
    }

    private async Task ActivateModifier(Player player, Rng rng)
    {
        MainFile.Logger.Info("Endless mode activated!");

        YuWanCard_HasStarted = true;

        if (LocalContext.IsMe(player))
        {
            await CreatureCmd.GainMaxHp(player.Creature, 10m);
        }
    }

    public override void AfterRunCreated(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier initialized. Loop: {YuWanCard_EndlessLoopCount}, TotalActs: {YuWanCard_TotalActsCleared}");
    }

    public override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier loaded. Loop: {YuWanCard_EndlessLoopCount}, TotalActs: {YuWanCard_TotalActsCleared}");
    }
}
```

**重要说明**：
- 使用 `[SavedProperty]` 标记需要持久化的属性
- BaseLib 会自动处理 SavedProperty 的注册，无需手动注入
- 使用 `LocalContext.IsMe(player)` 检查是否为本地玩家，避免多人游戏中重复执行
- 属性名建议使用前缀（如 `YuWanCard_`）以避免命名冲突

## Modifier 常用钩子方法

| 方法 | 说明 |
|------|------|
| `GenerateNeowOption(EventModel)` | 生成 Neow 事件选项 |
| `AfterRunCreated(RunState)` | Run 创建后调用 |
| `AfterRunLoaded(RunState)` | Run 加载后调用 |
| `AfterRoomEntered(AbstractRoom)` | 进入房间后调用 |
| `AfterCreatureAddedToCombat(Creature)` | 生物加入战斗后调用 |
| `ShouldAllowAncient(Player, AncientEventModel)` | 是否允许先古之民事件 |
| `ShouldAllowSelectingMoreCardRewards(Player)` | 是否允许选择更多卡牌奖励 |

## 注册 Modifier

需要通过 Harmony 补丁将 Modifier 注册到 `ModelDb` 和 `GoodModifiers` 列表：

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class ModifierRegistrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ModelDb.Contains(typeof(EndlessModifier)))
        {
            ModelDb.Inject(typeof(EndlessModifier));
            MainFile.Logger.Info("EndlessModifier registered to ModelDb");
        }
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
public class GoodModifiersPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
    {
        var list = __result.ToList();
        if (!list.Any(m => m is EndlessModifier))
        {
            list.Add(ModelDb.Modifier<EndlessModifier>());
            __result = list;
        }
    }
}
```

**注意**：BaseLib 会自动处理 `SavedProperty` 的注册，无需手动注入到 `SavedPropertiesTypeCache`。

## Modifier 本地化

本地化文件格式（`modifiers.json`）：

```json
{
  "MYMOD-ENDLESS.title": "无尽",
  "MYMOD-ENDLESS.description": "爬塔无止境，直至死亡。",
  "MYMOD-ENDLESS.neow_title": "无尽模式",
  "MYMOD-ENDLESS.neow_description": "获得 10 点最大生命。爬塔无止境，直至死亡。"
}
```

## 完整示例：无尽模式

以下是一个完整的无尽模式 Modifier 实现（基于项目实际代码）：

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

public class EndlessModifier : ModifierModel
{
    public const string ModifierId = "YUWANCARD-ENDLESS";

    private const float BaseHpMultiplierPerLoop = 0.20f;
    private const float BossHpMultiplierBonus = 0.10f;
    private const int BaseStrengthPerLoop = 1;
    private const int BossExtraStrengthPerLoop = 1;
    private const float HpGrowthExponent = 1.1f;

    [SavedProperty]
    public int YuWanCard_EndlessLoopCount { get; set; } = 0;

    [SavedProperty]
    public int YuWanCard_TotalActsCleared { get; set; } = 0;

    [SavedProperty]
    public bool YuWanCard_HasStarted { get; set; } = false;

    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");

    public override string IconPath => "res://YuWanCard/images/modifiers/endless.png";

    public int EffectiveLoopCount => Math.Max(0, YuWanCard_EndlessLoopCount);

    private float CalculateHpMultiplier(bool isBoss)
    {
        if (EffectiveLoopCount <= 0) return 1f;
        
        float baseMultiplier = 1f + (BaseHpMultiplierPerLoop * (float)Math.Pow(EffectiveLoopCount, HpGrowthExponent));
        
        if (isBoss)
        {
            baseMultiplier += BossHpMultiplierBonus * EffectiveLoopCount;
        }
        
        return baseMultiplier;
    }

    private int CalculateStrengthBonus(bool isBoss)
    {
        if (EffectiveLoopCount <= 0) return 0;
        
        int bonus = BaseStrengthPerLoop * EffectiveLoopCount;
        
        if (isBoss)
        {
            bonus += BossExtraStrengthPerLoop * (EffectiveLoopCount / 2);
        }
        
        return bonus;
    }

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

    private async Task ApplyDifficultyScaling(Creature creature, bool isBoss)
    {
        float hpMultiplier = CalculateHpMultiplier(isBoss);
        int strengthBonus = CalculateStrengthBonus(isBoss);

        int newMaxHp = (int)(creature.MaxHp * hpMultiplier);
        await CreatureCmd.SetMaxHp(creature, newMaxHp);
        await CreatureCmd.Heal(creature, newMaxHp - creature.CurrentHp, playAnim: false);

        if (strengthBonus > 0)
        {
            await PowerCmd.Apply<StrengthPower>(creature, (decimal)strengthBonus, null, null);
        }

        MainFile.Logger.Info($"Applied endless difficulty to {creature.ModelId} (Boss: {isBoss}): HP x{hpMultiplier:F2}, Strength +{strengthBonus}");
    }

    public override bool ShouldAllowAncient(Player player, AncientEventModel ancient)
    {
        if (ancient is Neow && EffectiveLoopCount > 0)
        {
            return false;
        }
        return true;
    }

    public override void AfterRunCreated(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier initialized. Loop: {YuWanCard_EndlessLoopCount}, TotalActs: {YuWanCard_TotalActsCleared}");
    }

    public override void AfterRunLoaded(RunState runState)
    {
        MainFile.Logger.Info($"Endless modifier loaded. Loop: {YuWanCard_EndlessLoopCount}, TotalActs: {YuWanCard_TotalActsCleared}");
    }

    public void IncrementLoopCount()
    {
        YuWanCard_EndlessLoopCount++;
        YuWanCard_TotalActsCleared++;
        MainFile.Logger.Info($"Endless loop incremented. Now at loop {YuWanCard_EndlessLoopCount}, total acts: {YuWanCard_TotalActsCleared}");
    }

    public void IncrementActCount()
    {
        YuWanCard_TotalActsCleared++;
    }

    public static EndlessModifier? GetEndlessModifier(RunState runState)
    {
        foreach (var modifier in runState.Modifiers)
        {
            if (modifier is EndlessModifier endlessModifier)
            {
                return endlessModifier;
            }
        }
        return null;
    }

    public static bool IsEndlessMode(RunState runState)
    {
        return GetEndlessModifier(runState) != null;
    }
}
```

**功能特性**：
- **难度递增**：每完成一次循环，敌人生命值和力量增加
- **BOSS 加成**：BOSS 敌人获得额外的生命值和力量加成
- **Neow 事件限制**：第一次循环后禁止 Neow 事件
- **持久化**：使用 SavedProperty 保存循环次数和章节进度
- **日志记录**：详细的日志输出用于调试
