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
| `None` | 无目标（不需要选择目标） |
| `Self` | 自身（自动对自己使用） |
| `AnyEnemy` | 任意敌人（单体） |
| `AllEnemies` | 所有敌人（AOE） |
| `RandomEnemy` | 随机敌人（自动选择） |
| `AnyAlly` | 任意队友（包括自己） |
| `AllAllies` | 所有队友（包括自己） |
| `AnyPlayer` | 任意玩家（可选中已死亡的队友） |
| `TargetedNoCreature` | 需要选择目标位置但不指向生物 |
| `Osty` | Osty 系统专用目标 |

使用 `DynamicEnumValueMinter` 可以创建自定义 `TargetType`（如 `Everyone`、`Anyone`），见 [扩展功能文档](11-extensions.md#自定义目标类型)。

### 卡牌类型 (CardType)

| 类型 | 说明 |
|------|------|
| `Attack` | 攻击牌 |
| `Skill` | 技能牌 |
| `Power` | 能力牌 |
| `Curse` | 诅咒牌（不可打出） |
| `Status` | 状态牌（不可打出，战斗中生成） |
| `Quest` | 任务牌 |

### 卡牌标签 (CardTag)

内置标签：

| 标签 | 说明 |
|------|------|
| `Strike` | 打击系（被"完美打击"等卡牌计算） |
| `Defend` | 防御系 |
| `Minion` | 仆从标签 |
| `OstyAttack` | Osty 攻击标签 |
| `Shiv` | 小刀标签 |

自定义标签使用 `ModCardTagRegistry` 创建（见 [工具类文档](05-utils.md#modcardtagregistry)）。

### 卡牌关键字 (CardKeyword)

内置关键字：

| 关键字 | 说明 |
|--------|------|
| `Exhaust` | 消耗（打出后从牌组移除） |
| `Ethereal` | 虚无（回合结束未打出则消耗） |
| `Innate` | 固有（战斗开始时在手牌中） |
| `Unplayable` | 不可打出 |
| `Retain` | 保留（回合结束时不弃牌） |
| `Sly` | 狡黠（特定条件下额外效果） |
| `Eternal` | 永恒（不进入弃牌堆/消耗堆） |

使用 `WithKeywords()` 和 `WithKeyword(keyword, UpgradeType.Add/Remove)` 管理关键字。

### 卡牌稀有度 (CardRarity)

| 稀有度 | 说明 | 出现方式 |
|--------|------|----------|
| `Basic` | 基础牌 | 初始牌组 |
| `Common` | 普通 | 常规战斗奖励 |
| `Uncommon` | 罕见 | 常规战斗奖励 |
| `Rare` | 稀有 | 常规战斗奖励 |
| `Ancient` | 先古之民 | 先古之民事件专属 |
| `Event` | 事件 | 事件专属获取 |
| `Token` | 衍生 | 通过其他卡牌/遗物生成 |
| `Curse` | 诅咒 | 事件/遗物负面效果 |
| `Status` | 状态 | 战斗中生成 |
| `Quest` | 任务 | 任务系统专属 |

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
    WithRepeat(3);                              // 设置重复次数
    WithPower<StrengthPower>(2);                // 设置能力层数
    WithPower<StrengthPower>("Venom", 3);       // 命名能力层数
    WithVar("MyVar", 3, 1);                     // 通用变量
    WithVars(var1, var2);                       // 多个变量
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

**计算伤害**：使用 `WithCalculatedDamage` 实现基于状态的伤害计算：

```csharp
public MyCard() : base(...)
{
    WithCalculatedDamage(
        ValueProp.Move,                           // 伤害属性
        (card, target) => card.CombatState?.Enemies?.Count ?? 0, // 倍率（如敌人数量）
        baseVal: 6,                               // 基础伤害
        extraVal: 0,                              // 每层倍率额外伤害
        baseUpgrade: 3,                           // 基础伤害升级值
        extraUpgrade: 0                           // 额外伤害升级值
    );
}
// 计算伤害 = (CalculationBase + CalculationExtra × 倍率) × 全局系数
// 使用示例：每个敌人造成 6 点伤害 → 3个敌人 = 18点
// 动态变量: {CalculatedDamage:diff()} 在本地化中使用
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

### 卡牌打出流程

卡牌打出时按以下顺序执行：

1. `CanPlay(PlayerChoiceContext, CardPlay)` — 检查是否可打出（返回 false 阻止打出）
2. 扣除能量、移动卡牌到打出区
3. `OnPlay(PlayerChoiceContext, CardPlay)` — 执行卡牌效果（异步）
4. 触发 `AfterCardPlayed` 钩子（能力、遗物等）

### 伤害命令链

`DamageCmd` 提供流式 API 构建和执行伤害：

```csharp
await DamageCmd.Attack(damage)           // 攻击伤害（受力量加成）
    .FromCard(this)                      // 来源卡牌（用于触发相关效果）
    .Targeting(target)                   // 目标生物
    .WithHitFx("vfx/vfx_attack_slash")   // 命中特效
    .WithHitCount(hitCount)              // 命中次数
    .SetUnblockable()                    // 设为不可格挡
    .SetUnpowered()                      // 设为不受能力影响
    .Execute(choiceContext);             // 执行

// 直接伤害（不受力量加成，如中毒）
await DamageCmd.DealDamage(amount)
    .FromCard(this)
    .Targeting(target)
    .Execute(choiceContext);
```

### 卡牌费用升级

```csharp
public MyCard() : base(baseCost: 2, ...)
{
    WithCostUpgradeBy(-1);  // 升级后费用 -1
}
public MyCard() : base(baseCost: 1, ...)
{
    CostUpgrade = 0;  // 升级后费用变为 0
}
```

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

### 命令系统 (Commands)

游戏使用命令模式执行所有操作，以下是最常用的命令类：

| 命令类 | 用途 | 常用方法 |
|--------|------|---------|
| `DamageCmd` | 造成伤害 | `.Attack()`, `.DealDamage()`, `.FromCard()`, `.Targeting()`, `.Execute()` |
| `CreatureCmd` | 生物操作 | `.GainBlock()`, `.Heal()`, `.GainMaxHp()`, `.SetMaxHp()`, `.Damage()` |
| `PowerCmd` | 能力操作 | `.Apply<T>()`, `.Remove<T>()`, `.ModifyAmount()` |
| `PlayerCmd` | 玩家操作 | `.GainGold()`, `.LoseGold()`, `.GainEnergy()` |
| `RelicCmd` | 遗物操作 | `.Obtain<T>()`, `.Remove<T>()` |
| `CardPileCmd` | 卡牌堆操作 | `.Add()`, `.Move()`, `.Remove()`, `.Draw()`, `.Discard()`, `.Exhaust()` |
| `ForgeCmd` | 锻造操作 | `.Forge()` |
| `SfxCmd` | 音效 | `.Play()`, `.PlayMusic()` |

所有命令方法都是异步的（返回 `Task`），需要使用 `await`。

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

### 能力本地化：description vs smartDescription

**能力有两种描述文本，使用规则不同**：

| 字段 | 使用场景 | 是否支持动态变量 |
|------|---------|---------------|
| `description` | 图鉴/卡牌预览中的能力描述（规范模型） | **不支持** DynamicVar，仅支持 `{Amount}` 等隐式变量 |
| `smartDescription` | 战斗中生物身上的能力悬浮提示（实例化后） | **支持** 所有 DynamicVar + 隐式变量 |
| `remoteDescription` | 多人游戏中其他玩家施加的能力提示 | **支持** 所有 DynamicVar + 隐式变量 |

**正确示例**：

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪疑惑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "每回合获得1个随机的[gold]能力[/gold]。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "每回合获得{PigDoubtPower}个随机的[gold]能力[/gold]。",
  
  "YUWANCARD-PIG_VAMPIRIC_POWER.title": "猪吸血",
  "YUWANCARD-PIG_VAMPIRIC_POWER.description": "你在本回合内，每打出一张攻击牌，恢复1点生命值。",
  "YUWANCARD-PIG_VAMPIRIC_POWER.smartDescription": "你在本回合内，每打出一张攻击牌，恢复{PigVampiricPower:diff()}点生命值。"
}
```

**关键规则**：
- `description` 写静态文本（因为加载规范模型时无法获取运行时动态变量）
- `smartDescription` 写动态变量（因为能力实例化后，`DynamicVars.AddTo()` 会被自动调用）
- 注意：**卡牌的 `description` 不同**，卡牌的 description 会自动调用 `DynamicVars.AddTo()`，所以卡牌 description 支持动态变量

### PowerType 类型

| 类型 | 说明 | 颜色 |
|------|------|------|
| `Buff` | 增益效果 | 绿色 |
| `Debuff` | 减益效果 | 红色 |
| `Neutral` | 中性效果 | 蓝色 |

### PowerStackType 类型

| 类型 | 说明 |
|------|------|
| `Counter` | 层数叠加（显示数字，可增减） |
| `Single` | 不叠加（施加后不显示数字，只存在或不存在） |
| `None` | 无叠加显示（能力存在但不显示层数） |

**注意**：Duration 的持续效果使用 `StackType = Counter`，然后利用 `SkipNextDurationTick` 控制每回合递减。

### 常用钩子方法

| 方法 | 说明 |
|------|------|
| `AfterApplied(Creature?, CardModel?)` | 能力被施加后 |
| `BeforeApplied(Creature?, CardModel?)` | 能力被施加前 |
| `AfterPowerAmountChanged(int, int)` | 能力层数变化后 |
| `AfterSideTurnStart(CombatSide, CombatState)` | 任意方回合开始时 |
| `AfterSideTurnEnd(CombatSide, CombatState)` | 任意方回合结束时 |
| `BeforeTurnStart(CombatTurn)` | 回合开始前 |
| `AfterPlayerTurnStart()` | 玩家回合开始时 |
| `AfterPlayerTurnEnd()` | 玩家回合结束时 |
| `ModifyDamage(decimal, DamageInfo, Creature)` | 修改伤害值 |
| `ModifyBlock(decimal, Creature)` | 修改格挡值 |
| `OnAttack(DamageInfo, Creature)` | 攻击时触发 |
| `OnAttacked(DamageInfo, Creature)` | 被攻击时触发 |
| `AfterCardPlayed(Card)` | 打出卡牌后 |
| `AfterCardDrawn(Card)` | 抽牌后 |
| `AfterCardExhausted(Card)` | 卡牌被消耗后 |
| `AfterCombatStarted()` | 战斗开始后 |
| `AfterCombatEnded()` | 战斗结束后 |
| `OnDeath()` | 拥有者死亡时 |
| `ShouldPlayVfx` | 是否播放视觉特效（可重写） |

### 隐式变量（smartDescription 中自动可用）

在 `smartDescription` 中，以下变量会自动注入，无需在 `CanonicalVars` 中定义：

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `{Amount}` | int | 当前能力层数 |
| `{Duration}` | int | 持续时间（Duration 类型时） |
| `{OnPlayer}` | bool | 拥有者是否为玩家 |
| `{IsMultiplayer}` | bool | 是否为多人游戏 |
| `{PlayerCount}` | int | 玩家数量 |
| `{OwnerName}` | string | 拥有者名称 |
| `{ApplierName}` | string | 施加者名称（可能为空） |
| `{TargetName}` | string | 目标名称（可能为空） |
| `{singleStarIcon}` | string | 星星图标 img 标签 |
| `{energyPrefix}` | string | 能量图标前缀 |

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
| `Starter` | 初始遗物 | 角色自带 |
| `Common` | 普通 | 50% |
| `Uncommon` | 罕见 | 35% |
| `Rare` | 稀有 | 15% |
| `Shop` | 商店遗物 | 仅商店购买 |
| `Event` | 事件遗物 | 事件专属获取 |
| `Ancient` | 先古之民 | 先古之民专属 |

### 常用钩子方法

**回合/战斗钩子**：
| 方法 | 说明 |
|------|------|
| `AfterObtained()` | 获得遗物时 |
| `AfterPlayerTurnStart()` | 玩家回合开始时 |
| `AfterPlayerTurnEnd()` | 玩家回合结束时 |
| `AfterCombatStarted()` | 战斗开始时 |
| `AfterCombatVictory()` | 战斗胜利后 |
| `AfterCombatDefeat()` | 战斗失败后 |
| `AfterRoomEntered(AbstractRoom)` | 进入房间后 |

**数值修改钩子**：
| 方法 | 说明 |
|------|------|
| `ModifyDamage(decimal, DamageInfo, Player)` | 修改伤害值（加法） |
| `ModifyDamageMultiplicative(decimal, Player)` | 修改伤害倍率 |
| `ModifyBlock(decimal, Player)` | 修改格挡值（加法） |
| `ModifyBlockMultiplicative(decimal, Player)` | 修改格挡倍率 |
| `ModifyMaxEnergy(Player, decimal)` | 修改最大能量 |
| `ModifyHandDraw(Player, int)` | 修改抽牌数 |
| `ModifyRestSiteHealAmount(Player, decimal)` | 修改休息处回复量 |

**卡牌/奖励钩子**：
| 方法 | 说明 |
|------|------|
| `AfterCardPlayed(Card)` | 打出卡牌后 |
| `AfterCardExhausted(Card)` | 卡牌被消耗后 |
| `TryModifyRewards(Rewards)` | 修改战斗奖励 |
| `ShouldGainGold(decimal, Player)` | 获得金币前（返回 false 阻止） |
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

### 药水稀有度 (PotionRarity)

| 稀有度 | 说明 |
|--------|------|
| `Common` | 普通药水 |
| `Uncommon` | 罕见药水 |
| `Rare` | 稀有药水 |
| `Event` | 事件专属药水 |
| `Token` | 衍生药水 |

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
