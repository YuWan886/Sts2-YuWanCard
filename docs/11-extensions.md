# 扩展功能

## 自定义 ID 属性

使用 `CustomIDAttribute` 覆盖默认的自动前缀生成：

```csharp
using YuWanCard.Core.Utils.Attributes;

[CustomID("MYMOD-CUSTOM_ID")]
public class MyCard : YuWanCardModel
{
    // 此卡牌的 ID 将是 "MYMOD-CUSTOM_ID"
}
```

**用途**：
- 与其他模组保持 ID 兼容
- 迁移旧版本 ID
- 特殊 ID 格式需求

---

## 生命条预测

在能力中实现 `IHealthBarForecastSource` 接口，可在生命条上显示预测效果：

```csharp
using MegaCrit.Sts2.Core.Entities.Powers;
using Godot;

public class MyPoisonPower : YuWanPowerModel, IHealthBarForecastSource
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

### HealthBarForecastSegment 参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `Amount` | `decimal` | 预测的 HP 变化量 |
| `Color` | `Color` | 预测条颜色 |
| `Direction` | `HealthBarForecastDirection` | 生长方向 |
| `Order` | `int` | 渲染顺序 |
| `OverlayMaterial` | `Material?` | 可选的覆盖材质 |

### 生长方向

| 值 | 说明 |
|------|------|
| `FromRight` | 从当前 HP 向内生长 |
| `FromLeft` | 从空白向外生长 |

### 毁灭条着色器

使用毁灭条着色器创建特殊效果：

```csharp
using YuWanCard.Core.Utils;

var material = ShaderUtils.CreateDoomBarShaderMaterial(
    ShaderUtils.CreateVanillaDoomBarGradientTexture()
);

yield return new HealthBarForecastSegment(
    Amount,
    color,
    direction,
    0,
    material
);
```

---

## 自定义游戏动作

用于多人游戏同步的自定义游戏动作：

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace YuWanCard.GameActions;

public class ShoppingCartPurchaseAction : GameAction
{
    private readonly Player _player;
    private readonly int _itemIndex;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public ShoppingCartPurchaseAction(Player player, int itemIndex = 0)
    {
        _player = player;
        _itemIndex = itemIndex;
    }

    protected override async Task ExecuteAction()
    {
        // 执行动作逻辑
    }

    public override INetAction ToNetAction()
    {
        return new NetShoppingCartPurchaseAction(_itemIndex);
    }
}

public struct NetShoppingCartPurchaseAction : INetAction, IPacketSerializable
{
    private int _itemIndex;

    public GameAction ToGameAction(Player owner)
    {
        return new ShoppingCartPurchaseAction(owner, _itemIndex);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(_itemIndex);
    }

    public void Deserialize(PacketReader reader)
    {
        _itemIndex = reader.ReadInt();
    }
}
```

---

## 多人游戏消息

用于自定义网络通信：

```csharp
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Multiplayer;

public struct TeammatePayRequestMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required int PurchaseId { get; set; }
    public required ulong RequesterNetId { get; set; }
    public required ulong TargetNetId { get; set; }
    public required int GoldAmount { get; set; }
    public required string EntryId { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(PurchaseId);
        writer.WriteULong(RequesterNetId);
        writer.WriteULong(TargetNetId);
        writer.WriteInt(GoldAmount);
        writer.WriteString(EntryId);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        PurchaseId = reader.ReadInt();
        RequesterNetId = reader.ReadULong();
        TargetNetId = reader.ReadULong();
        GoldAmount = reader.ReadInt();
        EntryId = reader.ReadString();
        Location = reader.Read<RunLocation>();
    }
}
```

---

## 自定义 UI

使用 Godot Controls 创建自定义 UI：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.UI;

public partial class MyPopup : Control, IScreenContext
{
    public Control? DefaultFocusedControl => null;

    public static MyPopup? Create()
    {
        var popup = new MyPopup();
        popup.SetAnchorsPreset(LayoutPreset.FullRect);
        popup.MouseFilter = MouseFilterEnum.Ignore;
        popup.SetupUI();
        return popup;
    }

    private void SetupUI()
    {
        var mainPanel = new PanelContainer();
        mainPanel.AnchorLeft = 0.5f;
        mainPanel.AnchorRight = 0.5f;
        mainPanel.OffsetLeft = -350f;
        mainPanel.OffsetRight = 350f;

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        styleBox.BorderColor = new Color(0.4f, 0.35f, 0.25f);
        styleBox.SetBorderWidthAll(3);
        styleBox.SetCornerRadiusAll(10);
        mainPanel.AddThemeStyleboxOverride("panel", styleBox);

        AddChild(mainPanel);
    }

    public void Open()
    {
        NModalContainer.Instance?.Add(this, showBackstop: true);
        SfxCmd.Play("event:/sfx/ui/ui_card_reward_open");
    }

    public void Close()
    {
        NModalContainer.Instance?.Clear();
        SfxCmd.Play("event:/sfx/ui/ui_button_click");
    }

    private static string GetLocText(string key) => new LocString("settings_ui", key).GetRawText();
}
```

---

## 遗物升级系统

实现可升级的遗物：

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class UpgradableRelic : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    [SavedProperty]
    public int YuWanCard_UpgradeLevel { get; set; } = 0;

    public int MaxUpgradeLevel => 3;

    public bool CanUpgrade => YuWanCard_UpgradeLevel < MaxUpgradeLevel;

    public override async Task AfterCombatVictory()
    {
        await base.AfterCombatVictory();
        
        if (CanUpgrade && SomeCondition())
        {
            YuWanCard_UpgradeLevel++;
            Flash();
            MainFile.Logger.Info($"Relic upgraded to level {YuWanCard_UpgradeLevel}");
        }
    }

    public override decimal ModifyDamageMultiplicative(decimal amount, Player player)
    {
        return player == Owner ? amount * (1m + 0.1m * YuWanCard_UpgradeLevel) : amount;
    }
}
```

---

## 自定义卡牌池

创建自定义卡牌池：

```csharp
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.CardPools;

public class PigCardPool : CustomCardPoolModel
{
    public override string Title => "pig_pool";
    public override bool IsShared => false;
    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return YuWanCardModel.RegisteredCards
            .Where(c => c is IYuWanCard)
            .ToArray();
    }
}
```

---

## 自定义遭遇

注册自定义遭遇：

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

    public override EncounterType Type => EncounterType.Normal;
    public override Act[] Acts => [Act.One];
}
```

---

## 自定义事件

创建自定义事件：

```csharp
using MegaCrit.Sts2.Core.Entities.Events;

namespace YuWanCard.Events;

public class PigEvent : EventModel
{
    public override string EventId => "pig_event";
    public override Act[] Acts => [Act.One, Act.Two];

    public override async Task<EventState> Initialize(Player player)
    {
        return new EventState(
            "page_1",
            new EventPage(
                new LocString("events", "PIG_EVENT.page_1.description"),
                [
                    new EventOption(
                        "option_1",
                        async () => {
                            await PlayerCmd.GainGold(50, player);
                            return "page_2";
                        },
                        new LocString("events", "PIG_EVENT.page_1.option_1.title")
                    ),
                    new EventOption(
                        "option_2",
                        async () => {
                            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), player.Creature, 5m);
                            return null;
                        },
                        new LocString("events", "PIG_EVENT.page_1.option_2.title")
                    ),
                ]
            )
        );
    }
}
```
