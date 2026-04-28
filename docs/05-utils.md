# 工具类

项目提供了多个工具类来简化常见操作。

## Core/Utils 目录

### CommonActions

常用卡牌动作的快捷方法：

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

**方法列表**：

| 方法 | 说明 |
|------|------|
| `CardAttack(CardModel, CardPlay, int)` | 创建攻击命令 |
| `CardBlock(CardModel, CardPlay?)` | 获得格挡 |
| `Apply<T>(PlayerChoiceContext, Creature, CardModel, decimal)` | 施加能力 |

### WeightedList

加权随机列表，支持按权重随机选择：

```csharp
using YuWanCard.Core.Utils;

var list = new WeightedList<string>();
list.Add("常见", 70);    // 70% 权重
list.Add("罕见", 25);    // 25% 权重
list.Add("稀有", 5);     // 5% 权重

var result = list.GetRandom(rng);          // 随机选择
var result2 = list.GetRandom(rng, remove: true);  // 随机选择并移除
```

**IWeighted 接口**：

实现 `IWeighted` 接口的对象可以自动使用其 `Weight` 属性：

```csharp
public class WeightedItem : IWeighted
{
    public int Weight { get; set; }
    public string Value { get; set; }
}

var list = new WeightedList<WeightedItem>();
list.Add(new WeightedItem { Weight = 10, Value = "test" });
```

### SpireField

基于 `ConditionalWeakTable` 的实例数据存储，用于在不修改类的情况下添加额外数据：

```csharp
using YuWanCard.Core.Utils;

// 创建字段
private static readonly SpireField<Creature, int> CustomCounter = new(() => 0);

// 获取值
int counter = CustomCounter.Get(creature);

// 设置值
CustomCounter.Set(creature, 5);

// 使用索引器
CustomCounter[creature] = 10;
```

**SavedSpireField**：

支持保存/加载的字段：

```csharp
private static readonly SavedSpireField<Creature, int> SavedCounter = 
    new(() => 0, "CustomCounter");
```

### ShaderUtils

着色器生成工具：

```csharp
using YuWanCard.Core.Utils;

// 创建毁灭条着色器材质
var material = ShaderUtils.CreateDoomBarShaderMaterial(
    ShaderUtils.CreateVanillaDoomBarGradientTexture()
);
```

### AncientDialogueUtil

先古之民对话本地化工具，用于生成本地化键：

```csharp
using YuWanCard.Core.Utils;

// 首次访问对话键
var key = AncientDialogueUtil.GetFirstVisitEverKey("pig_ancient", 0, 0);

// 角色对话键
var key2 = AncientDialogueUtil.GetCharacterDialogueKey("pig_ancient", "IRONCLAD", 1, 2);

// 通用对话键
var key3 = AncientDialogueUtil.GetGenericDialogueKey("pig_ancient", 0, 1);
```

### TooltipSource

悬停提示源，用于卡牌的额外提示：

```csharp
using YuWanCard.Core.Utils;

public override IEnumerable<TooltipSource> ExtraHoverTips
{
    get
    {
        yield return new TooltipSource(_ => HoverTipFactory.FromPower<MyPower>());
    }
}
```

---

## Utils 目录

### PowerSafetyUtils

能力安全性检查工具，用于检查能力是否可以安全地赋予玩家：

```csharp
using YuWanCard.Utils;

// 检查能力是否安全
if (PowerSafetyUtils.IsSafePower(power))
{
    await PowerCmd.Apply(target, amount, source, card);
}
```

**不安全的能力特征**：
- 包含怪物专属逻辑
- 调用 `MonsterModel` 类型转换
- 未正确处理 `dealer` 参数的空值检查
- 在 `UnsafePowerTypes` 列表中的能力

**排除模组自定义能力**：

```csharp
private bool IsSafePower(PowerModel power)
{
    // 排除模组自定义能力
    if (power is YuWanPowerModel)
        return false;
    
    return PowerSafetyUtils.IsSafePower(power);
}
```

### GoldModificationGuard

金币修改保护器，避免递归调用：

```csharp
using YuWanCard.Utils;

public class MyRelic : RelicModel
{
    private GoldModificationGuard? _goldGuard;

    private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
        () => Owner,
        amount => Math.Floor(amount * 0.5m),  // 扣除 50%
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
}
```

### GameVersionCompat

游戏版本兼容性工具：

```csharp
using YuWanCard.Utils;

// 获取游戏版本
var version = GameVersionCompat.GameVersion;

// 当前版本常量
var currentVersion = GameVersionCompat.CurrentVersion;
```

### PetManager

宠物管理器，用于管理玩家的宠物：

```csharp
using YuWanCard.Utils;

// 获取宠物
var pet = PetManager.GetPet(player);

// 添加宠物
PetManager.AddPet(player, petData);

// 移除宠物
PetManager.RemovePet(player);
```

### ShoppingCartManager

购物车管理器，用于多人游戏中的共享购物：

```csharp
using YuWanCard.Utils;

// 添加商品到购物车
ShoppingCartManager.AddItem(player, item);

// 移除商品
ShoppingCartManager.RemoveItem(player, itemId);

// 获取购物车
var cart = ShoppingCartManager.GetCart(player);
```

### UpdateChecker

模组更新检查器：

```csharp
using YuWanCard.Utils;

// 检查更新
var hasUpdate = await UpdateChecker.CheckForUpdate();

// 显示更新弹窗
if (hasUpdate)
{
    UpdatePopup.Show();
}
```

### CardUtils

卡牌相关工具方法：

```csharp
using YuWanCard.Utils;

// 检查卡牌是否为攻击牌
bool isAttack = CardUtils.IsAttack(card);

// 获取卡牌的实际伤害
decimal damage = CardUtils.GetActualDamage(card, target);
```

### VfxUtils

视觉特效工具：

```csharp
using YuWanCard.Utils;

// 播放特效
await VfxUtils.PlayEffect("vfx/vfx_attack_slash", target);

// 创建自定义特效
var effect = VfxUtils.CreateEffect("res://custom_vfx.tscn");
```

### NodeUtils

Godot 节点工具：

```csharp
using YuWanCard.Utils;

// 创建节点
var node = NodeUtils.CreateNode<Node2D>();

// 查找节点
var child = NodeUtils.FindNode(parent, "ChildName");
```

### AudioUtils

音频工具：

```csharp
using YuWanCard.Utils;

// 播放音效
await AudioUtils.PlaySfx("event:/sfx/ui/ui_button_click");

// 播放音乐
await AudioUtils.PlayMusic("event:/music/combat");
```

### YuWanReflectionHelper

反射辅助工具：

```csharp
using YuWanCard.Utils;

// 获取私有字段
var field = YuWanReflectionHelper.GetPrivateField(obj, "fieldName");

// 调用私有方法
var result = YuWanReflectionHelper.InvokePrivateMethod(obj, "methodName", args);
```
