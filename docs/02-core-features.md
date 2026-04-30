# 核心功能

## 卡牌系统

### 基类：YuWanCardModel

项目使用 `YuWanCardModel` 作为所有卡牌的基类，提供自动 ID 生成和资源路径管理。

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

### 构造函数参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `baseCost` | `int` | 基础费用 |
| `type` | `CardType` | 卡牌类型（Attack、Skill、Power、Curse、Status） |
| `rarity` | `CardRarity` | 稀有度（Basic、Common、Uncommon、Rare、Special） |
| `target` | `TargetType` | 目标类型 |

### TargetType 目标类型

| TargetType | 说明 |
|------------|------|
| `Self` | 自身 |
| `AllAllies` | 所有队友（包括自己） |
| `AnyAlly` | 任意队友（包括自己） |
| `AllEnemies` | 所有敌人 |
| `AnyEnemy` | 任意敌人 |
| `RandomEnemy` | 随机敌人 |
| `AnyPlayer` | 任意玩家（可用于选择死亡玩家） |
| `None` | 无目标 |

### 卡牌类型 (CardType)

| 类型 | 说明 |
|------|------|
| `Attack` | 攻击牌 |
| `Skill` | 技能牌 |
| `Power` | 能力牌 |
| `Curse` | 诅咒牌 |
| `Status` | 状态牌 |

### 卡牌稀有度 (CardRarity)

| 稀有度 | 说明 | 出现概率 |
|--------|------|----------|
| `Basic` | 基础牌 | 初始牌组 |
| `Common` | 普通 | 50% |
| `Uncommon` | 罕见 | 35% |
| `Rare` | 稀有 | 15% |
| `Special` | 特殊 | 特殊获取 |

### 链式 API

`YuWanCardModel` 提供链式 API 来设置卡牌属性：

```csharp
public MyCard() : base(...)
{
    WithDamage(6);                    // 设置伤害
    WithBlock(5);                     // 设置格挡
    WithHeal(3);                      // 设置治疗
    WithMagicNumber(2);               // 设置魔法数字
    WithTags(CardTag.Strike);         // 添加标签
    WithMultiDamage(3);               // 设置多段伤害
    WithPower<StrengthPower>(2);      // 设置能力层数
}
```

### 关键方法

| 方法 | 说明 |
|------|------|
| `OnPlay(PlayerChoiceContext, CardPlay)` | 打出卡牌时执行 |
| `OnUpgrade()` | 升级时调用 |
| `OnObtained()` | 获得卡牌时调用 |
| `OnExhausted()` | 消耗时调用 |
| `OnRetained()` | 保留时调用 |
| `CanPlay(PlayerChoiceContext, CardPlay)` | 是否可以打出 |

### 多人游戏限制

```csharp
public override CardMultiplayerConstraint MultiplayerConstraint 
    => CardMultiplayerConstraint.MultiplayerOnly;
```

| 约束类型 | 说明 |
|----------|------|
| `None` | 无限制 |
| `MultiplayerOnly` | 仅多人模式 |
| `SingleplayerOnly` | 仅单人模式 |

---

## 能力系统

### 基类：YuWanPowerModel

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
            await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
        }
    }
}
```

### PowerType 类型

| 类型 | 说明 | 颜色 |
|------|------|------|
| `Buff` | 增益效果 | 绿色 |
| `Debuff` | 减益效果 | 红色 |
| `Neutral` | 中性效果 | 蓝色 |

### PowerStackType 类型

| 类型 | 说明 |
|------|------|
| `Counter` | 层数叠加 |
| `Duration` | 持续时间 |
| `None` | 不叠加 |

### 常用钩子方法

| 方法 | 说明 |
|------|------|
| `AfterSideTurnStart(CombatSide, CombatState)` | 回合开始时 |
| `AfterSideTurnEnd(CombatSide, CombatState)` | 回合结束时 |
| `ModifyDamage(decimal, DamageInfo, Creature)` | 修改伤害 |
| `ModifyBlock(decimal, Creature)` | 修改格挡 |
| `OnAttack(DamageInfo, Creature)` | 攻击时触发 |
| `OnAttacked(DamageInfo, Creature)` | 被攻击时触发 |
| `AfterCardPlayed(Card)` | 打出卡牌后 |
| `AfterCardDrawn(Card)` | 抽牌后 |

---

## 遗物系统

### 基类：YuWanRelicModel

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
        await PowerCmd.Apply<StrengthPower>(Owner, 1, Owner, null);
    }
}
```

### 遗物稀有度

| 稀有度 | 说明 | 掉落率 |
|--------|------|--------|
| `Common` | 普通 | 50% |
| `Uncommon` | 罕见 | 35% |
| `Rare` | 稀有 | 15% |
| `Ancient` | 先古之民 | 特殊获取 |
| `Shop` | 商店 | 仅商店购买 |

### 常用钩子方法

| 方法 | 说明 |
|------|------|
| `AfterObtained()` | 获得遗物时 |
| `AfterPlayerTurnStart()` | 玩家回合开始时 |
| `AfterCombatVictory()` | 战斗胜利后 |
| `ModifyDamageMultiplicative(decimal, Player)` | 修改伤害倍率 |
| `ModifyBlockMultiplicative(decimal, Player)` | 修改格挡倍率 |
| `ModifyMaxEnergy(Player, decimal)` | 修改最大能量 |
| `ModifyHandDraw(Player, int)` | 修改抽牌数 |
| `ShouldGainGold(decimal, Player)` | 获得金币前 |
| `AfterGoldGained(Player)` | 获得金币后 |

---

## 角色系统

### 基类：PlaceholderCharacterModel

项目使用 `PlaceholderCharacterModel` 作为角色基类，它使用 Ironclad 作为占位符视觉：

```csharp
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Characters;

public class Pig : PlaceholderCharacterModel, IYuWanCharacter
{
    public override string CharacterId => "pig";
    public override string Title => "pig";

    public Pig()
    {
        StartingDeck =
        [
            typeof(PigStrike),
            typeof(PigStrike),
            typeof(PigStrike),
            typeof(PigDefend),
            typeof(PigDefend),
            typeof(PigDefend),
            typeof(PigDefend),
            typeof(PigBash),
        ];
    }

    public override RelicModel StartingRelic => ModelDb.Relic<PigCarrot>();
}
```

### IYuWanCharacter 接口

实现此接口可自定义角色视觉资源：

```csharp
public class Pig : PlaceholderCharacterModel, IYuWanCharacter
{
    public string? CustomVisualPath => "res://YuWanCard/scenes/pig_visual.tscn";
    public string? CustomCharacterSelectIconPath => "res://YuWanCard/images/characters/pig_select.png";
    public string? CustomIconPath => "res://YuWanCard/images/characters/pig_icon.png";
}
```

---

## 怪物系统

### 基类：YuWanMonsterModel

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YuWanCard.Monsters;

public class PigMinion : YuWanMonsterModel
{
    public override int MaxHp => 20;
    public override int MinHp => 15;

    public override async Task ExecuteTurn(PlayerChoiceContext choiceContext)
    {
        var action = SelectAction();
        switch (action)
        {
            case "attack":
                await DamageCmd.Attack(5)
                    .FromMonster(this)
                    .Targeting(choiceContext.CombatState.Player.Creature)
                    .Execute(choiceContext);
                break;
        }
    }
}
```

---

## 遭遇系统

### 自定义遭遇

```csharp
using MegaCrit.Sts2.Core.Models.Encounters;

namespace YuWanCard.Encounters;

public class PigEncounter : EncounterModel
{
    public override string EncounterId => "pig_battle";
    
    public override List<EncounterMonster> Monsters => 
    [
        new EncounterMonster(typeof(PigMinion), 0, 0),
    ];
}
```

---

## 附魔系统

### 基类：YuWanEnchantmentModel

```csharp
using MegaCrit.Sts2.Core.Entities.Enchantments;

namespace YuWanCard.Enchantments;

public class PigEnchantment : YuWanEnchantmentModel
{
    public override decimal Weight => 1.0m;
    
    public override bool CanEnchant(CardModel card)
    {
        return card.Type == CardType.Attack;
    }
    
    public override void ApplyEnchantment(Card card)
    {
        card.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
```

---

## 先古之民系统

### 自定义先古之民

```csharp
using MegaCrit.Sts2.Core.Entities.Ancients;

namespace YuWanCard.Ancients;

public class PigAncient : AncientModel
{
    public override string AncientId => "pig_ancient";
    
    public override async Task<List<AncientOption>> GenerateOptions(Player player)
    {
        return
        [
            new AncientOption(
                "option_1",
                async () => await RelicCmd.Obtain<PigCarrot>(player),
                new LocString("ancients", "PIG_ANCIENT.option_1.title"),
                new LocString("ancients", "PIG_ANCIENT.option_1.description")
            ),
        ];
    }
}
```

---

## 事件系统

### 自定义事件

```csharp
using MegaCrit.Sts2.Core.Entities.Events;

namespace YuWanCard.Events;

public class PigEvent : EventModel
{
    public override string EventId => "pig_event";
    public override Act[] Acts => [Act.One];
    
    public override async Task<EventState> Initialize(Player player)
    {
        return new EventState(
            "page_1",
            new EventPage(
                new LocString("events", "PIG_EVENT.page_1.description"),
                [
                    new EventOption(
                        "option_1",
                        async () => await PlayerCmd.GainGold(50, player),
                        new LocString("events", "PIG_EVENT.page_1.option_1.title")
                    ),
                ]
            )
        );
    }
}
```

---

## 充能球系统

### 基类：YuWanOrbModel

项目使用 `YuWanOrbModel` 作为所有充能球的基类，支持自动模型注册和自定义资源路径：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Entities.Orbs;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Orbs;

public class LittleRegentOrb : YuWanOrbModel
{
    public override Color DarkenedColor => new Color("FFD700");
    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 6m;

    public override string? CustomIconPath =>
        "res://YuWanCard/images/orbs/little_regent.png";

    public override string? CustomSpritePath =>
        "res://scenes/orbs/orb_visuals/plasma_orb.tscn";

    protected override string ChannelSfx =>
        "event:/sfx/characters/defect/defect_plasma_channel";

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await ForgeCmd.Forge(PassiveVal, Owner, this);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        await ForgeCmd.Forge(EvokeVal, Owner, this);
        return new[] { Owner.Creature };
    }
}
```

### 自定义资源路径

| 属性 | 说明 |
|------|------|
| `CustomIconPath` | 充能球图标的 Godot 资源路径 |
| `CustomSpritePath` | 自定义精灵场景的 Godot 资源路径（可选） |

### 充能球注册

继承 `YuWanOrbModel` 的充能球会自动通过 `ContentRegistry.AddModel` 注册。此外，项目通过 Harmony Patch 将自定义充能球注入到 `ModelDb.Orbs` 列表中，使其可被游戏识别：

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Orbs), MethodType.Getter)]
static class CustomOrbsListPatch
{
    [HarmonyPostfix]
    static IEnumerable<OrbModel> AddCustomOrbs(IEnumerable<OrbModel> __result)
    {
        return __result.Append(ModelDb.Orb<Orbs.LittleRegentOrb>());
    }
}
```

所有自定义充能球会自动出现在游戏的充能球数据库中，无需额外手动注册。

---

## 休息站选项

### 自定义休息站选项

```csharp
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace YuWanCard.RestSite;

public class PigRestSiteOption : RestSiteOption
{
    public override string OptionId => "pig_rest";
    
    public override async Task<bool> OnSelect()
    {
        await CreatureCmd.Heal(Owner.Creature, 10);
        return true;
    }
}
```

---

## 药水系统

### 基类：YuWanPotionModel

项目使用 `YuWanPotionModel` 作为所有药水的基类，支持自动模型注册和自定义资源路径：

```csharp
using MegaCrit.Sts2.Core.Entities.Potions;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Potions;

public class PigPotion : YuWanPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override string? CustomPackedImagePath =>
        "res://YuWanCard/images/potions/pig_potion.png";

    public override string? CustomPackedOutlinePath =>
        "res://YuWanCard/images/potions/pig_potion_outline.png";

    public override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext.Player, 2, choiceContext.Player, null);
    }
}
```

### 自定义资源路径

| 属性 | 说明 |
|------|------|
| `CustomPackedImagePath` | 药水主图标的 Godot 资源路径 |
| `CustomPackedOutlinePath` | 药水描边图的 Godot 资源路径（可选） |

使用 `YuWanPotionModel` 后，药水模型会自动通过 `ContentRegistry.AddModel` 注册，无需手动处理。

---

## Mod 联动（Interop）

项目提供了一套基于 Harmony Transpiler 的模组间互操作（Interop）框架，允许在**编译时不依赖外部模组 DLL** 的情况下，运行时动态调用其他模组的 API。

### 核心原理

1. 在本模组中定义"存根类"（Stub），包含与目标模组 API 签名相同的空实现方法
2. 使用 `[ModInterop]` 和 `[InteropTarget]` 特性标记目标模组和成员
3. 模组初始化时，`ModInteropProcessor` 扫描所有存根类
4. 若目标模组已加载，通过 Harmony Transpiler 将存根方法体替换为对目标模组的直接 IL 调用
5. 若目标模组未加载，保留空实现作为 fallback，不报错也不产生副作用

### 定义存根类

```csharp
using YuWanCard.Core.Interop;

namespace YuWanCard.Config.Interop;

[ModInterop("BaseLib")]  // 目标模组 ID
public static class BaseLibConfigInterop
{
    // 替换为 BaseLib.Config.ModConfigRegistry.Register 的调用
    [InteropTarget("BaseLib.Config.ModConfigRegistry", "Register")]
    public static void Register(string modId, object config)
    {
        // Fallback：目标模组未加载时不执行任何操作
    }
}
```

### 特性说明

| 特性 | 用途 |
|------|------|
| `[ModInterop(string modId, string? type)]` | 标记存根类，指定目标模组 ID 和默认类型上下文 |
| `[InteropTarget(string? type, string? name)]` | 标记存根成员，指定目标类型和成员名 |

`[InteropTarget]` 的参数规则：
- 提供 `type` 和 `name`：调用指定类型的指定成员
- 仅提供 `name`：沿用外层 `[ModInterop]` 指定的默认类型上下文

### 初始化处理

在模组初始化时调用 `ModInteropProcessor.Process`：

```csharp
public override void Initialize()
{
    ModInteropProcessor.Process(Harmony, typeof(MyMod).Assembly);
}
```

### 配置系统兼容层

项目已内置对以下模组的配置兼容：

| 模组 | 存根类 | 功能 |
|------|--------|------|
| BaseLib | `BaseLibConfigInterop` | 将本模组配置注册到 BaseLib 的设置界面 |
| STS2RitsuLib | `RitsuLibConfigInterop` | 将本模组配置注册到 RitsuLib 的设置界面，并同步数据存储 |

### 包装器类

当需要持有目标模组的实例对象时，可继承 `InteropClassWrapper`：

```csharp
public abstract class InteropClassWrapper
{
    public object Value = null!;
}
```

在存根类中定义嵌套类继承 `InteropClassWrapper`，`ModInteropProcessor` 会自动处理实例化与字段注入。
