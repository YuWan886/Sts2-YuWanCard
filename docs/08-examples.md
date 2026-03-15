# 示例

## 完整的模组示例

以下是一个完整的模组示例，展示了如何创建卡牌、遗物和能力：

### 模组入口文件

```csharp
// MainFile.cs
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("YuWanCard initialized");
    }
}
```

### 卡牌基类

```csharp
// YuWanCardModel.cs
using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Cards;

public abstract partial class YuWanCardModel(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://YuWanCard/images/card_portraits/{CardId}";

    public override string? CustomPortraitPath => $"{PortraitBasePath}.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
```

### 卡牌示例

```csharp
// PigHurt.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigHurt : YuWanCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    public PigHurt() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.AllEnemies
    )
    {
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VulnerablePower>(CombatState!.HittableEnemies, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(2);
    }
}
```

### 能力基类

```csharp
// YuWanPowerModel.cs
using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Powers;

public abstract class YuWanPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/powers/{PowerId}.png";

    public override string? CustomPackedIconPath => IconBasePath;
    public override string? CustomBigIconPath => IconBasePath;
}
```

### 能力示例

```csharp
// PigDoubtPower.cs
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            int powerCount = Amount;

            for (int i = 0; i < powerCount; i++)
            {
                var randomPower = GetRandomPower();
                if (randomPower != null)
                {
                    await PowerCmd.Apply(randomPower.ToMutable(), Owner, 1, Owner, null);
                }
            }
        }
    }

    private PowerModel? GetRandomPower()
    {
        var rng = Owner.Player?.RunState.Rng;
        if (rng == null) return null;

        var filteredPowers = ModelDb.AllPowers
            .Where(p => !p.IsInstanced && IsSafePower(p))
            .ToList();

        if (filteredPowers.Count == 0) return null;

        return rng.Niche.NextItem(filteredPowers);
    }

    private bool IsSafePower(PowerModel power)
    {
        var unsafePowers = new HashSet<string>
        {
            "MegaCrit.Sts2.Core.Models.Powers.CurlUpPower",
            "MegaCrit.Sts2.Core.Models.Powers.AngryPower",
        };
        return !unsafePowers.Contains(power.GetType().FullName ?? "");
    }
}
```

### 遗物基类

```csharp
// YuWanRelicModel.cs
using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace YuWanCard.Relics;

public abstract class YuWanRelicModel : CustomRelicModel
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://YuWanCard/images/relics/{RelicId}";

    public override string? PackedImagePath => $"{IconBasePath}.png";
    public override string? PackedOutlinePath => $"{IconBasePath}_outline.png";
}
```

## 本地化文件示例

### 卡牌本地化 (cards.json)

```json
{
  "YUWANCARD-PIG_HURT.title": "猪受伤",
  "YUWANCARD-PIG_HURT.description": "给予所有敌人 {Vulnerable:diff()} 层 [red]易伤[/red]。"
}
```

### 能力本地化 (powers.json)

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪疑惑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "回合开始时，获得 {amount} 个随机能力。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "回合开始时，获得 {amount} 个随机能力。"
}
```

### 遗物本地化 (relics.json)

```json
{
  "YUWANCARD-RING_OF_SEVEN_CURSES.title": "七咒之戒",
  "YUWANCARD-RING_OF_SEVEN_CURSES.description": "获得 1 个药水栏位。[gold]+1[/gold] 能量。[gold]+1[/gold] 抽牌数。受伤 [red]+50%[/red]。打 BOSS [gold]+50%[/gold] 伤害，打小怪 [red]-25%[/red] 伤害。格挡 [red]-20%[/red]。获得金币 [red]-50%[/red]。每回合获得一张诅咒牌。休息处回复 [red]-50%[/red]。BOSS 战后失去 [red]25%[/red] 最大生命。",
  "YUWANCARD-RING_OF_SEVEN_CURSES.flavor": "七重诅咒，七重力量。",
  "YUWANCARD-RING_OF_SEVEN_CURSES.additionalRestSiteHealText": "七咒之戒：回复 {ActualHeal} 点生命"
}
```
