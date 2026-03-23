# 核心功能

## 自定义卡牌

BaseLib 提供两种方式定义自定义卡牌：`CustomCardModel`（传统方式）和 `ConstructedCardModel`（链式 API 方式）。

### ConstructedCardModel（推荐）

`ConstructedCardModel` 是 BaseLib 新增的替代性卡牌基类，提供流畅的链式 API 来构建卡牌属性，大幅简化卡牌定义：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyCard() : ConstructedCardModel(
    cost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }

    protected override async Task UseCard(CardUser user, Creature? target, int stacks, CancellationToken ct)
    {
        await Damage(user, target, stacks);
    }
}
```

#### 链式 API 方法

| 方法 | 说明 |
|------|------|
| `WithVars(params DynamicVar[] vars)` | 添加多个动态变量 |
| `WithVar(string name, int baseVal)` | 添加命名变量 |
| `WithBlock(int baseVal)` | 生成 BlockVar |
| `WithDamage(int baseVal)` | 生成 DamageVar |
| `WithCards(int baseVal)` | 生成 CardsVar |
| `WithPower<T>(int baseVal)` | 生成 PowerVar<T> 并添加工具提示 |
| `WithPower<T>(string name, int baseVal)` | 带名称的 PowerVar |
| `WithTags(params CardTag[] tags)` | 添加卡牌标签 |
| `WithKeywords(params CardKeyword[] keywords)` | 添加关键词 |
| `WithCalculatedVar(...)` | 添加计算变量（伤害/格挡） |
| `WithTip(TooltipSource tipSource)` | 添加工具提示 |
| `WithEnergyTip()` | 添加能量提示 |

#### 完整示例

```csharp
[Pool(typeof(ColorlessCardPool))]
public class StrikeCard() : ConstructedCardModel(
    cost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> Vars => base.Vars
        .WithDamage(6);

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task UseCard(CardUser user, Creature? target, int stacks, CancellationToken ct)
    {
        await Damage(user, target, stacks);
    }
}

[Pool(typeof(ColorlessCardPool))]
public class DefendCard() : ConstructedCardModel(
    cost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    protected override IEnumerable<DynamicVar> Vars => base.Vars
        .WithBlock(5);

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }

    protected override async Task UseCard(CardUser user, Creature? target, int stacks, CancellationToken ct)
    {
        await Block(user, stacks);
    }
}
```

### CustomCardModel（传统方式）

继承 `CustomCardModel` 来创建自定义卡牌：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyCustomCard : CustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m)];

    public MyCustomCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.Enemy
    )
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attackCmd = CommonActions.CardAttack(this, cardPlay, hitCount: 1);
        await choiceContext.RunCommand(attackCmd);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
```

### 构造函数参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `baseCost` | int | 基础费用 |
| `type` | CardType | 卡牌类型（Attack、Skill、Power、Status、Curse、Quest） |
| `rarity` | CardRarity | 稀有度（Common、Uncommon、Rare、Ancient） |
| `target` | TargetType | 目标类型 |
| `showInCardLibrary` | bool | 是否在卡牌库中显示（默认 true） |
| `autoAdd` | bool | 是否自动添加到内容字典（默认 true） |

### TargetType 目标类型

| TargetType | 说明 |
|------------|------|
| `TargetType.Self` | 自身 |
| `TargetType.AllAllies` | 所有队友（包括自己） |
| `TargetType.AnyAlly` | 任意队友（包括自己） |
| `TargetType.AllEnemies` | 所有敌人 |
| `TargetType.AnyEnemy` | 任意敌人 |
| `TargetType.RandomEnemy` | 随机敌人 |
| `TargetType.AnyPlayer` | 任意玩家（可用于选择死亡玩家） |
| `TargetType.None` | 无目标 |

### 重要属性和方法

| 属性/方法 | 说明 |
|-----------|------|
| `GainsBlock` | 自动检测卡牌是否有格挡效果 |
| `CustomFrame` | 自定义卡牌框贴图（可选） |
| `CustomPortraitPath` | 自定义卡牌立绘路径（可选） |
| `CanonicalVars` | 定义卡牌的动态变量 |
| `OnPlay` | 卡牌打出时的逻辑 |
| `OnUpgrade` | 卡牌升级时的逻辑 |
| `MultiplayerConstraint` | 多人游戏限制（默认 `CardMultiplayerConstraint.None`） |

### CardMultiplayerConstraint 多人游戏限制

| 值 | 说明 |
|----|------|
| `CardMultiplayerConstraint.None` | 无限制（默认值），单人/多人模式都会出现 |
| `CardMultiplayerConstraint.MultiplayerOnly` | 仅限多人游戏，单人模式不会出现 |

### DynamicVar 常用类型

| 类型 | 说明 |
|------|------|
| `DamageVar(decimal)` | 伤害变量 |
| `BlockVar(decimal, ValueProp)` | 格挡变量 |
| `HealVar(decimal)` | 治疗变量 |
| `EnergyVar(decimal)` | 能量变量 |
| `PowerVar<TPower>(decimal)` | 能力层数变量 |
| `CardsVar(decimal)` | 卡牌数量变量 |
| `HpLossVar(decimal)` | 生命损失变量 |
| `MaxHpVar(decimal)` | 最大生命值变量 |
| `GoldVar(decimal)` | 金币值变量 |
| `IntVar(string, decimal)` | 简单整数变量 |
| `RepeatVar(decimal)` | 重复次数变量 |
| `CalculatedDamageVar(decimal, decimal, decimal)` | 计算伤害（基础+额外*倍率） |
| `CalculatedBlockVar(decimal, decimal, decimal)` | 计算格挡 |

### CardKeyword 常用类型

| 关键词 | 说明 |
|--------|------|
| `CardKeyword.Exhaust` | 消耗 |
| `CardKeyword.Innate` | 固有 |
| `CardKeyword.Ethereal` | 虚无 |
| `CardKeyword.Retain` | 保留 |

使用 `CanonicalKeywords` 属性返回卡牌关键词，使用 `AddKeyword(CardKeyword)` 和 `RemoveKeyword(CardKeyword)` 方法在升级时修改关键词。

### ExtraHoverTips 能力提示显示

使用 `ExtraHoverTips` 属性可以在卡牌悬停时显示额外信息（如卡牌给予的能力介绍）：

```csharp
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : CustomCardModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<RainDarkPower>()
    ];
}
```

**常用 HoverTips 类型**：

| 方法 | 说明 |
|------|------|
| `HoverTipFactory.FromPower<TPower>()` | 显示能力介绍 |
| `HoverTipFactory.Static(StaticHoverTip.Block)` | 显示格挡图标 |
| `HoverTipFactory.FromKeyword(CardKeyword)` | 显示关键词介绍 |
| `base.EnergyHoverTip` | 显示能量图标（继承自基类） |

**完整示例**：

```csharp
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : CustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<IntangiblePower>(3m),
        new PowerVar<RainDarkPower>(3m)
    ];

    // 显示卡牌给予的能力介绍
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<RainDarkPower>()
    ];

    public RainDark() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer)
            .ToList();

        foreach (var teammate in teammates)
        {
            await CreatureCmd.SetCurrentHp(teammate, 10m);
            await PowerCmd.Apply<IntangiblePower>(teammate, 3m, Owner.Creature, this);
            await PowerCmd.Apply<RainDarkPower>(teammate, 3m, Owner.Creature, this);
            
            var player = teammate.Player;
            if (player != null && player.PlayerCombatState != null)
            {
                int currentEnergy = player.PlayerCombatState.Energy;
                if (currentEnergy > 0)
                {
                    await PlayerCmd.GainEnergy(currentEnergy, player);
                }

                var hand = MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand.GetPile(player);
                int cardsToDraw = 10 - hand.Cards.Count;
                if (cardsToDraw > 0)
                {
                    await CardPileCmd.Draw(choiceContext, cardsToDraw, player);
                }
            }
        }
    }

    public override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
```

**注意事项**：
- `ExtraHoverTips` 属性会在卡牌悬停时显示在卡牌描述下方
- 对于给予能力的卡牌，强烈建议添加对应的能力 HoverTips
- 对于包含格挡、能量等效果的卡牌，可以添加对应的静态图标提示
- HoverTips 会按照列表顺序依次显示

## 自定义角色 (CustomCharacterModel)

继承 `CustomCharacterModel` 来创建自定义角色：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;

public class MyCustomCharacter : CustomCharacterModel
{
    public MyCustomCharacter()
    {
        Name = "My Character";
        StartingHealth = 70;
        StartingGold = 99;
        AttackAnimDelay = 0.15f;
        CastAnimDelay = 0.25f;
    }

    public override string? CustomVisualPath => "res://scenes/creature_visuals/my_character.tscn";
    public override string? CustomIconPath => "res://textures/icons/my_character.png";
}
```

### 视觉资源属性

| 属性 | 说明 |
|------|------|
| `CustomVisualPath` | 角色视觉场景路径（默认查找 `res://scenes/creature_visuals/class_name.tscn`） |
| `CustomTrailPath` | 卡牌轨迹路径 |
| `CustomIconTexturePath` | 小图标（保存运行信息弹窗用） |
| `CustomIconPath` | 左上角图标和图鉴筛选图标 |
| `CustomEnergyCounterPath` | 能量计数器场景路径 |
| `CustomRestSiteAnimPath` | 休息处动画路径 |
| `CustomMerchantAnimPath` | 商人动画路径 |
| `CustomCharacterSelectBg` | 角色选择背景场景 |
| `CustomCharacterSelectIconPath` | 角色选择图标 |
| `CustomCharacterSelectLockedIconPath` | 角色选择锁定图标 |
| `CustomMapMarkerPath` | 地图标记路径 |
| `CustomAttackSfx` | 攻击音效 |
| `CustomCastSfx` | 施法音效 |
| `CustomDeathSfx` | 死亡音效 |

### 默认值

| 属性 | 默认值 |
|------|--------|
| `StartingGold` | 99 |
| `AttackAnimDelay` | 0.15f |
| `CastAnimDelay` | 0.25f |

### 视觉场景要求

- 如果不重写 `CustomVisualPath`，则需要在 `res://scenes/creature_visuals/` 目录下创建名为 `class_name.tscn` 的场景文件
- 角色选择背景场景路径：`res://scenes/screens/char_select/char_select_bg_class_name.tscn`
- 场景必须包含以下节点：`Visuals`、`Bounds`、`IntentPos`、`CenterPos`、`OrbPos`、`TalkPos`

### 自定义能量计数器

可以使用 `CustomEnergyCounter` 结构体来自定义能量计数器的外观：

```csharp
public override CustomEnergyCounter? CustomEnergyCounter => new(
    layer => $"res://textures/energy/my_character_energy_layer{layer}.png",
    outlineColor: new Color(1, 0, 0),
    burstColor: new Color(1, 0.5f, 0)
);
```

`CustomEnergyCounter` 构造函数参数：
- `pathFunc`：`Func<int, string>` - 根据层数（1-5）返回对应贴图路径
- `outlineColor`：`Color` - 轮廓颜色
- `burstColor`：`Color` - 能量爆发粒子颜色

### SetupAnimationState 静态方法

`SetupAnimationState` 是一个静态辅助方法，用于简化动画状态设置：

```csharp
public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
{
    return SetupAnimationState(
        controller,
        idleName: "idle",           // 待机动画名称
        deadName: "dead",           // 死亡动画名称
        deadLoop: false,            // 死亡动画是否循环
        hitName: "hit",             // 受击动画名称
        hitLoop: false,             // 受击动画是否循环
        attackName: "attack",       // 攻击动画名称
        attackLoop: false,          // 攻击动画是否循环
        castName: "cast",           // 施法动画名称
        castLoop: false,            // 施法动画是否循环
        relaxedName: "relaxed",     // 放松动画名称
        relaxedLoop: true           // 放松动画是否循环
    );
}
```

### CreateCustomVisuals 方法

可以重写此方法创建自定义视觉组件：

```csharp
public override NCreatureVisuals? CreateCustomVisuals()
{
    if (CustomVisualPath == null) return null;
    return GodotUtils.CreatureVisualsFromScene(CustomVisualPath);
}
```

## 自定义遗物 (CustomRelicModel)

继承 `CustomRelicModel` 来创建自定义遗物：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

[Pool(typeof(SharedRelicPool))]
public class MyCustomRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
    }
}
```

### 遗物稀有度

| 稀有度 | 说明 |
|--------|------|
| `RelicRarity.Common` | 普通 |
| `RelicRarity.Uncommon` | 罕见 |
| `RelicRarity.Rare` | 稀有 |
| `RelicRarity.Ancient` | 先古之民 |
| `RelicRarity.Shop` | 商店 |

### 常用钩子方法

| 方法 | 说明 |
|------|------|
| `AfterObtained()` | 获得遗物时触发 |
| `AfterPlayerTurnStart(PlayerChoiceContext, Player)` | 玩家回合开始时触发 |
| `AfterCombatVictory(CombatRoom)` | 战斗胜利后触发 |
| `ModifyDamageMultiplicative(Creature?, decimal, ValueProp, Creature?, CardModel?)` | 修改伤害倍率 |
| `ModifyBlockMultiplicative(Creature, decimal, ValueProp, CardModel?, CardPlay?)` | 修改格挡倍率 |
| `ModifyMaxEnergy(Player, decimal)` | 修改最大能量 |
| `ModifyHandDraw(Player, decimal)` | 修改抽牌数 |
| `ModifyRestSiteHealAmount(Creature, decimal)` | 修改休息处回复血量 |
| `ModifyExtraRestSiteHealText(Player, IReadOnlyList<LocString>)` | 修改休息处额外文本 |
| `TryModifyRewards(Player, List<Reward>, AbstractRoom?)` | 修改奖励 |
| `ShouldGainGold(decimal, Player)` | 获得金币前触发 |
| `AfterGoldGained(Player)` | 获得金币后触发 |
| `ShouldAllowSelectingMoreCardRewards(Player)` | 是否允许选择更多卡牌奖励 |
| `GetUpgradeReplacement()` | 返回升级替换的遗物（默认 null） |

### 遗物升级替换

遗物可以设置升级替换（被 Orobas 先古之民遗物升级事件升级时）：

```csharp
public override RelicModel? GetUpgradeReplacement()
{
    return new MyUpgradedRelic();
}
```

**完整遗物示例**：

```csharp
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Rooms;

[Pool(typeof(SharedRelicPool))]
public class RingOfSevenCurses : CustomRelicModel
{
    private decimal _pendingGoldReduction;
    private bool _isApplyingReduction;

    [SavedProperty]
    private bool PotionSlotsAdded { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (!PotionSlotsAdded)
        {
            Owner?.AddToMaxPotionCount(1);
            PotionSlotsAdded = true;
        }
    }

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        if (_isApplyingReduction || player != Owner) return true;
        _pendingGoldReduction = Math.Floor(amount * 0.5m);
        return true;
    }

    public override async Task AfterGoldGained(Player player)
    {
        if (player == Owner && !_isApplyingReduction && _pendingGoldReduction > 0m)
        {
            decimal reduction = _pendingGoldReduction;
            _pendingGoldReduction = 0m;
            _isApplyingReduction = true;
            await PlayerCmd.LoseGold(reduction, player);
            _isApplyingReduction = false;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        
        var availableCurses = ModelDb.CardPool<CurseCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .ToHashSet();
        if (availableCurses.Count == 0) return;

        Flash();
        CardModel? curseCard = Owner.RunState.Rng.Niche.NextItem(availableCurses);
        if (curseCard != null && Owner.Creature.CombatState != null)
        {
            CardModel card = Owner.Creature.CombatState.CreateCard(curseCard, Owner);
            await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, addedByPlayer: true);
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != null && target.Player == Owner) return 1.5m;
        if (dealer != null && dealer.Player == Owner)
        {
            if (target != null && target.CombatState?.Encounter?.RoomType == RoomType.Boss) return 1.5m;
            return 0.75m;
        }
        return 1m;
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target.Player == Owner) return 0.8m;
        return 1m;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + 1 : amount;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return player == Owner ? count + 1 : count;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room == null) return false;

        if (room.RoomType == RoomType.Monster || room.RoomType == RoomType.Boss)
        {
            rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
        }
        if (room.RoomType == RoomType.Monster && Owner.RunState.Rng.Niche.NextFloat() < 0.5f)
        {
            rewards.Add(new RelicReward(player));
        }
        return true;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        return (creature.Player == Owner || creature.PetOwner == Owner) ? amount * 0.5m : amount;
    }

    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText)
    {
        if (player != Owner) return currentExtraText;
        var list = new List<LocString>(currentExtraText);
        var extraText = new LocString("relics", "MYMOD-RING_OF_SEVEN_CURSES.additionalRestSiteHealText");
        list.Add(extraText);
        return list;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (room?.RoomType != RoomType.Boss || Owner == null) return;
        int maxHpLoss = (int)Math.Floor(Owner.Creature.MaxHp * 0.25m);
        if (maxHpLoss > 0)
        {
            Flash();
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, maxHpLoss, isFromCard: false);
        }
    }
}
```

## 自定义能力 (CustomPowerModel)

继承 `CustomPowerModel` 来创建自定义能力：

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

public class MyCustomPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png";
    public override string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";

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

### 图标尺寸

| 属性 | 尺寸 |
|------|------|
| `CustomPackedIconPath` | 64x64 像素 |
| `CustomBigIconPath` | 256x256 像素 |
| `CustomBigBetaIconPath` | 256x256 像素（Beta 版本图标） |

### PowerType 类型

| 类型 | 说明 |
|------|------|
| `PowerType.Buff` | 增益效果（绿色） |
| `PowerType.Debuff` | 减益效果（红色） |
| `PowerType.Neutral` | 中性效果（蓝色） |

### PowerStackType 类型

| 类型 | 说明 |
|------|------|
| `PowerStackType.Counter` | 层数叠加 |
| `PowerStackType.Duration` | 持续时间 |
| `PowerStackType.None` | 不叠加 |

### 常用事件方法

| 方法 | 说明 |
|------|------|
| `AfterSideTurnStart(CombatSide, CombatState)` | 回合开始时触发 |
| `AfterSideTurnEnd(CombatSide, CombatState)` | 回合结束时触发 |
| `OnApply(Creature, int)` | 能力被应用时触发 |
| `OnRemove()` | 能力被移除时触发 |
| `Flash()` | 闪烁能力图标（视觉反馈） |

### ICustomPower 接口

如果你的能力需要继承自其他能力类（而不是直接继承 `PowerModel`），可以实现 `ICustomPower` 接口：

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

public class MyCustomPower : SomeOtherPower, ICustomPower
{
    public string? CustomPackedIconPath => "res://MyMod/images/powers/my_power.png";
    public string? CustomBigIconPath => "res://MyMod/images/powers/my_power.png";
    public string? CustomBigBetaIconPath => null;
}
```

**ICustomPower 接口属性**：

| 属性 | 说明 |
|------|------|
| `CustomPackedIconPath` | 小图标路径（64x64 像素） |
| `CustomBigIconPath` | 大图标路径（256x256 像素） |
| `CustomBigBetaIconPath` | Beta 版本大图标路径（256x256 像素，可选） |

**说明**：
- `CustomPowerModel` 同时继承了 `PowerModel` 和 `ICustomPower`，适合大多数情况
- `ICustomPower` 接口适合需要继承其他能力类的情况
- 实现此接口时，必须提供至少 `CustomPackedIconPath` 和 `CustomBigIconPath`

## 自定义卡牌池 (CustomCardPoolModel)

继承 `CustomCardPoolModel` 来创建自定义卡牌池：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;

public class MyCustomCardPool : CustomCardPoolModel
{
    public MyCustomCardPool()
    {
        Name = "My Card Pool";
    }

    public override bool IsShared => false;

    public override Texture2D? CustomFrame(CustomCardModel card) => null;

    public override Color ShaderColor => new Color(1, 0, 0);

    public override float H => ShaderColor.H;
    public override float S => ShaderColor.S;
    public override float V => ShaderColor.V;

    protected override CardModel[] GenerateAllCards() => [];
}
```

**重要说明**：
- 所有卡牌池必须是角色池或共享池，否则无法被找到
- 角色池通过 `CharacterModel.CardPool` 属性获取
- 共享池通过 `ModelDb.AllSharedCardPools` 获取
- `IsShared` 为 true 时，池会自动注册到 `ModelDb.AllSharedCardPools`
- `CustomFrame` 用于自定义卡牌框贴图（Ancient 稀有度忽略此逻辑）
- 如果不重写 `CardFrameMaterialPath`，会自动使用 `ShaderColor` 生成 HSV 着色器材质
- 可通过 `BigEnergyIconPath` 和 `TextEnergyIconPath` 自定义能量图标

## 自定义药水 (CustomPotionModel)

继承 `CustomPotionModel` 来创建自定义药水：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

[Pool(typeof(SharedPotionPool))]
public class MyCustomPotion : CustomPotionModel
{
    public MyCustomPotion()
    {
        Name = "My Potion";
        Description = "Heal 20 HP.";
    }

    public override bool AutoAdd => true;

    public override string? PackedImagePath => "res://textures/potions/my_potion.png";
    public override string? PackedOutlinePath => "res://textures/potions/my_potion_outline.png";
}
```

**属性说明**：
- `AutoAdd`：是否自动添加到内容字典（默认 true）
- `PackedImagePath`：药水贴图路径
- `PackedOutlinePath`：药水轮廓贴图路径

## 自定义先古之民 (CustomAncientModel)

继承 `CustomAncientModel` 来创建自定义先古之民事件：

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.Acts;

public class MyCustomAncient : CustomAncientModel
{
    public MyCustomAncient() : base(autoAdd: true)
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    protected override OptionPools MakeOptionPools => new(
        MakePool(ModelDb.Relic<SomeRelic>()),
        MakePool(ModelDb.Relic<AnotherRelic>())
    );

    public override string? CustomScenePath => "res://MyMod/scenes/ancients/my_ancient.tscn";
    public override string? CustomMapIconPath => "res://MyMod/images/ancients/my_ancient.png";
    public override string? CustomMapIconOutlinePath => "res://MyMod/images/ancients/my_ancient_outline.png";
    public override Texture2D? CustomRunHistoryIcon => GD.Load<Texture2D>("res://MyMod/images/ui/run_history/my_ancient.png");
    public override Texture2D? CustomRunHistoryIconOutline => GD.Load<Texture2D>("res://MyMod/images/ui/run_history/my_ancient_outline.png");
}
```

**重要方法**：
- `IsValidForAct(ActModel act)`：检查先古之民是否适用于指定章节（推荐使用 `act.Id == ModelDb.Act<Hive>().Id` 检查）
- `ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)`：是否强制生成此先古之民（谨慎使用，可能导致模组冲突）
- `MakeOptionPools`：创建选项池（抽象属性，必须实现）
- `GenerateInitialOptions()`：生成初始选项列表（可选重写）

**选项池工具**：
- `MakePool(params RelicModel[] options)`：从遗物模型创建加权列表
- `MakePool(params AncientOption[] options)`：从先古之民选项创建加权列表
- `AncientOption<T>(int weight, ...)`：创建先古之民选项（支持遗物预处理和变体）

**自定义选项示例**：

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new(this, ChooseCard, "MYMOD-MY_ANCIENT.pages.INITIAL.options.CHOOSE_CARD"),
        new(this, ChooseRelic, "MYMOD-MY_ANCIENT.pages.INITIAL.options.CHOOSE_RELIC"),
        new(this, UpgradeCards, "MYMOD-MY_ANCIENT.pages.INITIAL.options.UPGRADE_CARDS")
    };
}

private async Task ChooseCard()
{
    var cards = GetCustomCards();
    var cardReward = new CardReward(cardsToOffer, CardCreationSource.Other, Owner!);
    await RewardsCmd.OfferCustom(Owner!, [cardReward]);
    FinishEvent();
}

private void FinishEvent()
{
    var doneMethod = typeof(AncientEventModel).GetMethod("Done", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    doneMethod?.Invoke(this, null);
}
```

**本地化**：
先古之民事件使用特定的本地化键格式：
- 标题：`{ModId}-{AncientId}.title`
- 称号：`{ModId}-{AncientId}.epithet`
- 页面描述：`{ModId}-{AncientId}.pages.{PageName}.description`
- 选项：`{ModId}-{AncientId}.pages.{PageName}.options.{OptionId}.title` / `.description`
- 首次访问：`{ModId}-{AncientId}.talk.firstvisitEver.0-0.ancient`
- 角色对话：`{ModId}-{AncientId}.talk.{CharacterId}.{index}-{line}.ancient`
- 通用对话：`{ModId}-{AncientId}.talk.ANY.{index}-{line}.ancient`

**自定义场景**：
- 场景文件应放在 `res://MyMod/scenes/ancients/` 目录
- 需要配合 Harmony 补丁修改 `EventModel.BackgroundScenePath` 属性

### AncientOption 工具

`AncientOption` 是先古之民选项的抽象类：

```csharp
using BaseLib.Utils;

// 从遗物创建基础选项（隐式转换）
AncientOption option = ModelDb.Relic<MyRelic>();

// 创建带权重的选项
var weightedOption = new AncientOption<MyRelic>(weight: 2);

// 创建带预处理和变体的选项
var advancedOption = new AncientOption<MyRelic>(weight: 1)
{
    ModelPrep = relic => relic.Setup(),
    Variants = relic => new[] { relic, relic.UpgradedVersion }
};
```

**AncientOption 属性**：

| 属性 | 描述 |
|------|------|
| `Weight` | 选项权重，影响随机选择概率 |
| `AllVariants` | 所有变体遗物列表 |
| `ModelForOption` | 当前选项对应的遗物模型 |
| `ModelPrep` | 遗物预处理函数 |
| `Variants` | 变体生成函数 |

### OptionPools 构造

`OptionPools` 有三种构造方式：

```csharp
using BaseLib.Utils;

// 方式1：三个独立池（每个选项使用独立池）
var pools1 = new OptionPools(pool1, pool2, pool3);

// 方式2：两个池（前两个选项共用一个池）
var pools2 = new OptionPools(pool12, pool3);

// 方式3：单个池（所有选项共用一个池）
var pools3 = new OptionPools(pool);

// 获取所有选项
var allOptions = pools.AllOptions;

// 随机抽取选项
var selectedOptions = pools.Roll(rng);
```

## 自定义药水池 (CustomPotionPoolModel)

继承 `CustomPotionPoolModel` 来创建自定义药水池：

```csharp
using BaseLib.Abstracts;

public class MyCustomPotionPool : CustomPotionPoolModel
{
    public MyCustomPotionPool()
    {
        Name = "My Potion Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<PotionModel> GenerateAllPotions() => [];
}
```

**能量图标属性**：
- `BigEnergyIconPath`：大能量图标路径
- `TextEnergyIconPath`：文本能量图标路径

## 自定义遗物池 (CustomRelicPoolModel)

继承 `CustomRelicPoolModel` 来创建自定义遗物池：

```csharp
using BaseLib.Abstracts;

public class MyCustomRelicPool : CustomRelicPoolModel
{
    public MyCustomRelicPool()
    {
        Name = "My Relic Pool";
    }

    public override bool IsShared => false;

    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
```

**能量图标属性**：
- `BigEnergyIconPath`：大能量图标路径
- `TextEnergyIconPath`：文本能量图标路径
