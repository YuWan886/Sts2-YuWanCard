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

### 流式构建器 API

`YuWanCardModel` 提供链式 API 来设置卡牌属性：

```csharp
public MyCard() : base(...)
{
    WithDamage(6);                              // 设置伤害
    WithDamage(6, 3);                           // 设置伤害和升级值
    WithBlock(5);                               // 设置格挡
    WithBlock(5, 3);                            // 设置格挡和升级值
    WithHeal(3);                                // 设置治疗
    WithEnergy(1);                              // 设置能量
    WithCards(3);                               // 设置卡牌数量
    WithPower<StrengthPower>(2);                // 设置能力层数
    WithPower<StrengthPower>("Venom", 3);       // 命名能力层数
    WithVar("MyVar", 3, 1);                     // 通用变量
    WithVars(var1, var2);                       // 多个变量
    WithCalculatedDamage(props, calc);          // 计算伤害
    WithTags(CardTag.Strike);                   // 添加标签
    WithKeywords(CardKeyword.Ethereal);         // 添加关键字
    WithKeyword(CardKeyword.Innate, UpgradeType.Add);    // 升级时添加关键字
    WithKeyword(CardKeyword.Ethereal, UpgradeType.Remove); // 升级时移除关键字
    WithTip(_ => HoverTipFactory.FromPower<MyPower>());   // 悬停提示
    WithTip(new TooltipSource(...));             // TooltipSource 提示
    WithTip(typeof(StrengthPower));              // 按类型自动提示
    WithTip(CardKeyword.Strike);                 // 关键字提示
    WithTips(...);                               // 多个提示
    WithEnergyTip();                             // 能量提示
    WithCostUpgradeBy(-1);                       // 升级时费用变化
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

### 自定义肖像和边框

```csharp
// 重写 CustomPortraitPath 使用自定义肖像
public override string? CustomPortraitPath => "res://YuWanCard/images/card_portraits/my_card.png";

// 重写 CustomFrame 使用自定义边框
public override Texture2D? CustomFrame => ResourceLoader.Load<Texture2D>(
    "res://YuWanCard/images/card_frames/my_frame.png");
```

默认根据 `CardId` 查找 `images/card_portraits/{CardId}.png` 和 `images/card_frames/{CardId}.png`，如果文件不存在则回退到默认肖像。

### 超脱卡牌 (Transcendence)

实现 `ITranscendenceCard` 接口：

```csharp
public class MyCard : YuWanCardModel, ITranscendenceCard
{
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<MySuperCard>();
}
```

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
| `Single` | 不叠加 |
| `None` | 不叠加 |
| `Intensity` | 强度叠加 |
| `Duration` | 持续时间 |

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

### 临时能力

使用 `YuWanTemporaryPowerModelWrapper` 一行实现：

```csharp
// 临时能力本体包装器（应用时转换为 InternalPower）
public class PigChargePower : YuWanTemporaryPowerModelWrapper<PigCharge, StrengthPower>;
```

使用 `YuWanTemporaryPowerModel` 实现复杂逻辑：

```csharp
public class MyTempPower : YuWanTemporaryPowerModel
{
    public override PowerModel InternallyAppliedPower => ModelDb.Power<StrengthPower>();
    
    public override async Task BeforeApplied(Creature? applier, CardModel? cardSource)
    {
        await base.BeforeApplied(applier, cardSource);
        // 自定义应用前逻辑
    }
}
```

### 生命条预测

`YuWanPowerModel` 默认实现了 `IHealthBarForecastSource`，可在生命条上显示预测效果：

```csharp
using Godot;

public class MyPoisonPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (context.Creature == Owner && Amount > 0)
        {
            yield return new HealthBarForecastSegment(
                Amount,
                new Color(0.5f, 0.2f, 0.8f),
                HealthBarForecastDirection.FromRight,
                Order: 0
            );
        }
    }
}
```

**使用毁灭条着色器**：
```csharp
var material = ShaderUtils.CreateDoomBarShaderMaterial(
    ShaderUtils.CreateVanillaDoomBarGradientTexture()
);
yield return new HealthBarForecastSegment(Amount, color, direction, 0, material);
```

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

    public PigCarrot() : base(autoAdd: true) { }  // autoAdd=true 自动注册

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
| `Starter` | 初始 | 角色自带 |

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
| `ModifyRestSiteHealAmount(Player, decimal)` | 修改休息处回复 |
| `TryModifyRewards(Rewards)` | 修改奖励 |
| `ShouldGainGold(decimal, Player)` | 获得金币前 |
| `AfterGoldGained(Player)` | 获得金币后 |

### 遗物升级链

```csharp
// 基础遗物
public class PigCarrot : YuWanRelicModel
{
    public PigCarrot() : base(true) { }
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<GoldenCarrot>();
}

// 升级版遗物
public class GoldenCarrot : YuWanRelicModel
{
    public GoldenCarrot() : base(false) { }  // autoAdd=false，不自动注册
}
```

---

## 角色系统

### 基类：YuWanCharacterModel

项目使用 `YuWanCharacterModel` 或直接实现 `IYuWanCharacter` 接口作为角色基类：

```csharp
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Characters;

public class Pig : CharacterModel, IYuWanCharacter
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

    // IYuWanCharacter 接口实现
    string? IYuWanCharacter.CustomVisualPath => "res://YuWanCard/scenes/characters/pig.tscn";
    string? IYuWanCharacter.CustomEnergyCounterPath => "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";
    string? IYuWanCharacter.CustomCharacterSelectIconPath => "res://YuWanCard/images/characters/char_select_pig.png";
    string? IYuWanCharacter.CustomIconPath => "res://YuWanCard/scenes/ui/character_icons/pig_icon.tscn";
    string? IYuWanCharacter.CustomMerchantAnimPath => "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    string? IYuWanCharacter.CustomRestSiteAnimPath => "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";
}
```

### IYuWanCharacter 接口

实现此接口可自定义角色视觉资源（20+ 个路径属性）：

```csharp
public class Pig : CharacterModel, IYuWanCharacter
{
    // 自定义视觉效果路径
    string? IYuWanCharacter.CustomVisualPath => "res://YuWanCard/scenes/characters/pig.tscn";
    string? IYuWanCharacter.CustomEnergyCounterPath => "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";
    string? IYuWanCharacter.CustomMerchantAnimPath => "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    string? IYuWanCharacter.CustomRestSiteAnimPath => "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";
    
    // 角色选择界面
    string? IYuWanCharacter.CustomCharacterSelectIconPath => "res://YuWanCard/images/characters/char_select_pig.png";
    string? IYuWanCharacter.CustomCharacterSelectLockedIconPath => "res://YuWanCard/images/characters/char_select_pig.png";
    string? IYuWanCharacter.CustomCharacterSelectBg => "res://YuWanCard/scenes/characters/char_select_bg_pig.tscn";
    
    // UI 图标
    string? IYuWanCharacter.CustomIconPath => "res://YuWanCard/scenes/ui/character_icons/pig_icon.tscn";
    string? IYuWanCharacter.CustomIconTexturePath => "res://YuWanCard/images/characters/character_icon_pig.png";
    string? IYuWanCharacter.CustomIconOutlineTexturePath => "res://YuWanCard/images/characters/character_icon_pig.png";
    
    // 多人游戏手势
    string? IYuWanCharacter.CustomArmPointingTexturePath => "res://YuWanCard/images/characters/multiplayer_hand/pig_point.png";
    
    // 音效
    string? IYuWanCharacter.CustomAttackSfx => null;
    string? IYuWanCharacter.CustomCastSfx => null;
    string? IYuWanCharacter.CustomDeathSfx => null;
    
    // 动画
    CreatureAnimator? IYuWanCharacter.SetupCustomAnimationStates(MegaSprite controller) { ... }
}
```

**角色选择界面自动集成**：`CharacterSelectScreenPatch` 和 `CharacterSelectMonitor` 自动确保自定义角色按钮出现在角色选择屏幕上。

---

## 角色池设计

```csharp
[Pool(typeof(PigCardPool))]
public class PigCardPool : YuWanCardPoolModel
{
    public override string? BigEnergyIconPath =>
        "res://YuWanCard/images/characters/pig_enery_counter.png";
    public override string? TextEnergyIconPath =>
        "res://YuWanCard/images/characters/pig_text_enery.png";
    public override Color ShaderColor => new("F5C48C");   // 卡牌边框 HSV 色调
    public override bool IsShared => false;
    public override Color DeckEntryCardColor => new("FAFAD2");
    public override Color EnergyOutlineColor => new("773726");
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

public class PigEncounter : YuWanEncounterModel
{
    public PigEncounter() : base(RoomType.Monster)
    {
    }

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

public class PigAncient : YuWanAncientModel
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

public class PigEvent : YuWanEventModel
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

### 基类：CustomOrbModel

项目使用 `CustomOrbModel` 作为所有充能球的基类（继承自 `YuWanOrbModel`）：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YuWanCard.Orbs;

public class LittleRegentOrb : CustomOrbModel
{
    public override Color DarkenedColor => new Color("FFD700");
    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 6m;

    public override string? CustomIconPath => "res://YuWanCard/images/orbs/little_regent.png";

    public override string? CustomChannelSfx => "event:/sfx/characters/defect/defect_plasma_channel";

    public override Node2D? CreateCustomSprite()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/orbs/orb_visuals/plasma_orb.tscn");
        if (scene == null) return null;
        return scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
    }

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
| `CustomChannelSfx` | 充能音效路径 |
| `CreateCustomSprite()` | 创建自定义精灵场景 |

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
