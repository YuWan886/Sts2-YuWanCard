# 示例代码

## 卡牌示例

### 基础攻击牌

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(SharedCardPool))]
public class PigStrike : YuWanCardModel
{
    public PigStrike() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Basic,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6);
        WithTags(CardTag.Strike);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

### 基础技能牌

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(SharedCardPool))]
public class PigDefend : YuWanCardModel
{
    public PigDefend() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Basic,
        target: TargetType.Self)
    {
        WithBlock(5);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
```

### 多目标攻击牌

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(SharedCardPool))]
public class PigCleave : YuWanCardModel
{
    public PigCleave() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AllEnemies)
    {
        WithDamage(8);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in choiceContext.CombatState.Enemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

### 能力牌

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(SharedCardPool))]
public class PigStrengthPowerCard : YuWanCardModel
{
    public PigStrengthPowerCard() : base(
        baseCost: 1,
        type: CardType.Power,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<StrengthPower>(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.GetPowerVar<StrengthPower>()!.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.GetPowerVar<StrengthPower>()?.UpgradeValueBy(1m);
    }
}
```

---

## 能力示例

### 回合开始触发能力

```csharp
using MegaCrit.Sts2.Core.Entities.Powers;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new DynamicVar("PigDoubtPower", 1m)];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            var randomPower = GetRandomSafePower(combatState.Rng);
            if (randomPower != null)
            {
                await PowerCmd.Apply(randomPower, Amount, Owner.Creature, null);
            }
        }
    }

    private PowerModel? GetRandomSafePower(Rng rng)
    {
        var allPowers = ModelDb.GetAllPowers()
            .Where(IsSafePower)
            .ToList();
        
        if (allPowers.Count == 0) return null;
        return allPowers[rng.NextInt(allPowers.Count)];
    }

    private bool IsSafePower(PowerModel power)
    {
        if (power is YuWanPowerModel) return false;
        return PowerSafetyUtils.IsSafePower(power);
    }
}
```

### 伤害修改能力

```csharp
using MegaCrit.Sts2.Core.Entities.Powers;

namespace YuWanCard.Powers;

public class PigStrengthPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamage(decimal amount, DamageInfo? damageInfo, Creature? target)
    {
        return amount + Amount;
    }
}
```

---

## 遗物示例

### 回合开始触发遗物

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class PigCarrot : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterPlayerTurnStart()
    {
        await base.AfterPlayerTurnStart();
        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1, Owner.Creature, null);
    }
}
```

### 战斗胜利触发遗物

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class PigTreasure : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterCombatVictory()
    {
        await base.AfterCombatVictory();
        Flash();
        await PlayerCmd.GainGold(10, Owner);
    }
}
```

### 带状态的遗物

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class PigCounter : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    [SavedProperty]
    public int YuWanCard_Counter { get; set; } = 0;

    public override async Task AfterPlayerTurnStart()
    {
        await base.AfterPlayerTurnStart();
        YuWanCard_Counter++;
        
        if (YuWanCard_Counter >= 3)
        {
            Flash();
            YuWanCard_Counter = 0;
            await PlayerCmd.GainEnergy(1, Owner);
        }
    }
}
```

---

## 修改器示例

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

---

## Harmony 补丁示例

### 添加 Neow 选项

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Events;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Neow))]
class NeowPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialOptions")]
    static void AddCustomOption(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        var options = __result.ToList();
        options.Add(new EventOption(
            __instance,
            async () => {
                await RelicCmd.Obtain<PigCarrot>(__instance.Owner);
            },
            new LocString("events", "PIG_NEOW_OPTION.title"),
            new LocString("events", "PIG_NEOW_OPTION.description")
        ));
        __result = options;
    }
}
```

### 修改战斗奖励

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CombatRoom))]
class CombatRoomPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("GenerateRewards")]
    static void AddExtraGold(CombatRoom __instance)
    {
        if (__instance.Owner?.HasRelic<PigTreasure>() == true)
        {
            __instance.Rewards.Gold += 10;
        }
    }
}
```

---

## 本地化示例

### cards.json

```json
{
  "YUWANCARD-PIG_STRIKE.title": "猪猪打击",
  "YUWANCARD-PIG_STRIKE.description": "造成 {Damage:diff()} 点伤害。",
  
  "YUWANCARD-PIG_DEFEND.title": "猪猪防御",
  "YUWANCARD-PIG_DEFEND.description": "获得 {Block:diff()} 点 [gold]格挡[/gold]。",
  
  "YUWANCARD-PIG_CLEAVE.title": "猪猪横扫",
  "YUWANCARD-PIG_CLEAVE.description": "对所有敌人造成 {Damage:diff()} 点伤害。"
}
```

### powers.json

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪猪怀疑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "每回合开始时，获得 {PigDoubtPower:diff()} 个随机 [gold]能力[/gold]。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "每回合获得随机能力。"
}
```

### relics.json

```json
{
  "YUWANCARD-PIG_CARROT.title": "猪猪胡萝卜",
  "YUWANCARD-PIG_CARROT.description": "每回合开始时，获得 1 点 [red]力量[/red]。",
  "YUWANCARD-PIG_CARROT.flavor": "一根新鲜的胡萝卜，猪猪很喜欢。",
  
  "YUWANCARD-PIG_TREASURE.title": "猪猪宝藏",
  "YUWANCARD-PIG_TREASURE.description": "战斗胜利后，获得 [gold]10[/gold] 金币。",
  "YUWANCARD-PIG_TREASURE.flavor": "猪猪藏起来的宝藏。"
}
```
