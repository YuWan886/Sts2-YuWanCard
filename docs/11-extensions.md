# 扩展功能

## 自定义 UI

### 弹窗 UI

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

### 角色选择监视器

`CharacterSelectMonitor` 是一个运行时轮询节点，自动发现场景中的 `NCharacterSelectScreen` 和 `NCardLibrary` 并注入自定义内容：

```csharp
// 在 MainFile.Initialize() 中安装
CharacterSelectMonitor.TryInstall();
```

---

## 自定义游戏动作

用于多人游戏同步：

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

## 视觉特效 (Vfx)

自定义视觉特效位于 `YuWanCardCode/Vfx/`：

```csharp
using YuWanCard.Utils;

// 使用 VfxUtils 创建特效
VfxUtils.CreatePigCrashEffect(target);  // 猪坠机特效
```

---

## 徽章 (Badges)

继承框架的 `CustomBadgeRegistry` 注册，在 `YuWanCardCode/Badges/` 中实现：

```csharp
// 在 MainFile.Initialize() 中注册
CustomBadgeRegistry.Register(() => new PigTycoonBadge());
```

---

## 自定义目标类型

使用 `DynamicEnumValueMinter` 创建自定义目标类型：

```csharp
using YuWanCard.Core.Utils;

// 创建自定义目标类型
public static class CustomTargetType
{
    public static readonly TargetType Everyone = (TargetType)DynamicEnumValueMinter.MintValue("Everyone");
    public static readonly TargetType Anyone = (TargetType)DynamicEnumValueMinter.MintValue("Anyone");
}
```

---

## 自定义卡牌标签

使用 `ModCardTagRegistry` 创建自定义卡牌标签：

```csharp
using YuWanCard.Core.Utils;

// 获取模组注册表并创建新标签
var registry = ModCardTagRegistry.For("YUWANCARD");
var myTag = registry.RegisterOwned("MY_TAG");  // → YUWANCARD-MY_TAG

// 在卡牌中使用
WithTags(YuWanTags.FoodPig, myTag);
```

---

## 模组互操作（Interop）

使用 `[ModInterop]` 特性实现与其他模组的互操作：

```csharp
using YuWanCard.Core.Interop;

[ModInterop("STS2-RitsuLib")]
public static class RitsuInteropExample
{
    [InteropTarget("STS2RitsuLib.RitsuLibFramework", "GetDataStore")]
    public static object? GetDataStore(string modId)
    {
        // Fallback：目标模组未加载时不执行任何操作
        return null;
    }
}
```

在模组初始化时调用 `ModInteropProcessor.Process`：

```csharp
public override void Initialize()
{
    ModInteropProcessor.Process(Harmony, typeof(MyMod).Assembly);
}
```

---

## 自定义着色器

使用 `ShaderUtils` 创建着色器：

```csharp
using YuWanCard.Core.Utils;

// 创建毁灭条着色器材质
var material = ShaderUtils.CreateDoomBarShaderMaterial(
    ShaderUtils.CreateVanillaDoomBarGradientTexture()
);
```

---

## 自定义动画状态

在角色中实现 `SetupCustomAnimationStates`：

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;

public class Pig : CharacterModel, IYuWanCharacter
{
    CreatureAnimator? IYuWanCharacter.SetupCustomAnimationStates(MegaSprite controller)
    {
        var animator = new CreatureAnimator(controller);
        
        // 添加自定义动画状态
        animator.AddState("idle", "animation_idle");
        animator.AddState("attack", "animation_attack");
        animator.AddState("hit", "animation_hit");
        
        return animator;
    }
}
```

---

## 自定义音效

在角色中实现 `CustomAttackSfx`、`CustomCastSfx`、`CustomDeathSfx`：

```csharp
public class Pig : CharacterModel, IYuWanCharacter
{
    string? IYuWanCharacter.CustomAttackSfx => "event:/sfx/characters/pig/pig_attack";
    string? IYuWanCharacter.CustomCastSfx => "event:/sfx/characters/pig/pig_cast";
    string? IYuWanCharacter.CustomDeathSfx => "event:/sfx/characters/pig/pig_death";
}
```

---

## 自定义充能球精灵

在充能球中实现 `CreateCustomSprite`：

```csharp
public class LittleRegentOrb : CustomOrbModel
{
    public override Node2D? CreateCustomSprite()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/orbs/orb_visuals/plasma_orb.tscn");
        if (scene == null) return null;
        return scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
    }
}
```

---

## 自定义休息站选项

实现 `IYuWanRestSiteOption` 接口或继承 `RestSiteOption`：

```csharp
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace YuWanCard.RestSite;

public sealed class RoastPorkRestSiteOption(Player owner) : RestSiteOption(owner)
{
    private static readonly string CustomIconPath = "res://YuWanCard/images/ui/rest_site/option_roast_pork.png";

    public override string OptionId => "ROAST_PORK";

    public override IEnumerable<string> AssetPaths => [CustomIconPath];

    public override LocString Description
    {
        get
        {
            LocString locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
            locString.Add("HpLoss", 3m);
            locString.Add("CardCount", 1m);
            return locString;
        }
    }

    public override async Task<bool> OnSelect()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, 3m, 
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        var allPlayers = Owner.RunState.Players;
        foreach (var player in allPlayers)
        {
            if (player != Owner && player.Creature.CurrentHp > 0)
            {
                var pigChopCard = Owner.RunState.CreateCard(ModelDb.Card<PigChop>(), player);
                await CardPileCmd.Add(pigChopCard, PileType.Deck);
            }
        }

        return true;
    }
}
```
