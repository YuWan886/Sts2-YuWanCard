# BBCode 与占位符

Slay the Spire 2 使用 `RichTextLabel` 显示文本，支持 BBCode 标记语法和占位符系统。

## Godot 原生 BBCode

由于使用 `RichTextLabel`，Godot 原生的 BBCode 都可以使用。详细参考 [Godot 官方文档](https://docs.godotengine.org/zh-cn/4.x/tutorials/ui/bbcode_in_richtextlabel.html)。

### 常用标签

| BBCode | 说明 | 示例 |
| --- | --- | --- |
| `[b]...[/b]` | 粗体 | `[b]bold[/b]` |
| `[i]...[/i]` | 斜体 | `[i]italic[/i]` |
| `[u]...[/u]` | 下划线 | `[u]underline[/u]` |
| `[color=...]...[/color]` | 文字颜色 | `[color=red]red text[/color]` |
| `[font=...]...[/font]` | 字体 | `[font=Arial]Arial text[/font]` |
| `[size=...]...[/size]` | 字号 | `[size=24]large text[/size]` |
| `[img]...[/img]` | 图片 | `[img]res://path/to/image.png[/img]` |
| `[url=...]...[/url]` | 超链接 | `[url=https://example.com]link[/url]` |
| `[center]...[/center]` | 居中 | `[center]centered text[/center]` |
| `[left]...[/left]` | 左对齐 | `[left]left aligned[/left]` |
| `[right]...[/right]` | 右对齐 | `[right]right aligned[/right]` |

## 游戏自定义标签

游戏扩展了自定义 BBCode 标签：

| 标签名 | 作用 |
| --- | --- |
| `[ancient_banner]...[/ancient_banner]` | 古代横幅风格 |
| `[aqua]...[/aqua]` | 水绿色文字 |
| `[blue]...[/blue]` | 蓝色文字 |
| `[fade_in]...[/fade_in]` | 渐显动画效果 |
| `[fly_in]...[/fly_in]` | 飞入动画效果 |
| `[gold]...[/gold]` | 金色文字 |
| `[green]...[/green]` | 绿色文字 |
| `[jitter]...[/jitter]` | 抖动动画效果 |
| `[orange]...[/orange]` | 橙色文字 |
| `[pink]...[/pink]` | 粉色文字 |
| `[purple]...[/purple]` | 紫色文字 |
| `[red]...[/red]` | 红色文字 |
| `[sine]...[/sine]` | 正弦波动动画效果 |
| `[thinky_dots]...[/thinky_dots]` | 思考点点动画效果 |

### 颜色标签示例

```json
{
  "MYMOD-CARD.description": "造成 [red]{Damage:diff()}[/red] 点伤害，获得 [green]{Block:diff()}[/green] 点格挡。"
}
```

## 占位变量

占位变量会被 model 中的 `DynamicVars` 对应数值替换。所有占位变量都继承自 `DynamicVar` 基类。

### DynamicVar 基类

`DynamicVar` 是所有动态变量的基类，定义在 `MegaCrit.Sts2.Core.Localization.DynamicVars` 命名空间。

**核心属性**：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 变量名称，用于在本地化字符串中引用 |
| `BaseValue` | decimal | 基础数值 |
| `EnchantedValue` | decimal | 附魔后的数值 |
| `PreviewValue` | decimal | 预览显示的数值（考虑所有修正后） |
| `IntValue` | int | BaseValue 的整数形式 |
| `WasJustUpgraded` | bool | 标记是否刚刚升级过（用于高亮显示） |

**核心方法**：

| 方法 | 说明 |
|------|------|
| `ResetToBase()` | 重置为基础值 |
| `UpgradeValueBy(decimal addend)` | 升级时增加数值 |
| `UpdateCardPreview(CardModel, CardPreviewMode, Creature?, bool)` | 更新卡牌预览显示（子类重写） |
| `Clone()` | 克隆变量 |
| `ToHighlightedString(bool inverse)` | 返回带颜色高亮的字符串 |

### 基础变量

| 名称 | 对应类 | 说明 | 示例 |
| --- | --- | --- | --- |
| `{Damage}` | `DamageVar` | 伤害 | `造成{Damage:diff()}点伤害。` |
| `{Block}` | `BlockVar` | 格挡 | `获得{Block:diff()}点格挡。` |
| `{Cards}` | `CardsVar` | 卡牌数量 | `抽{Cards:diff()}张牌。` |
| `{Energy}` | `EnergyVar` | 能量（动态值） | `获得{Energy:energyIcons()}。` |
| `{Repeat}` | `RepeatVar` | 重复次数 | `造成{Damage:diff()}点伤害{Repeat:diff()}次。` |
| `{Heal}` | `HealVar` | 治疗 | `回复{Heal:diff()}点生命。` |
| `{HpLoss}` | `HpLossVar` | 失去生命 | `失去{HpLoss:inverseDiff()}点生命。` |
| `{MaxHp}` | `MaxHpVar` | 最大生命 | `获得{MaxHp:diff()}点最大生命。` |
| `{Gold}` | `GoldVar` | 金币 | `获得{Gold:diff()}金币。` |
| `{Summon}` | `SummonVar` | 召唤 | `召唤{Summon:diff()}。` |
| `{Forge}` | `ForgeVar` | 铸造 | `铸造{Forge:diff()}。` |
| `{Stars}` | `StarsVar` | 辉星 | `获得{Stars:starIcons()}。` |

### 能力变量

能力变量使用 `PowerVar<T>` 泛型类，其中 `T` 是能力类型。

| 名称 | 说明 | 示例 |
| --- | --- | --- |
| `{StrengthPower}` | 力量 | `获得{StrengthPower:diff()}点力量。` |
| `{DexterityPower}` | 敏捷 | `获得{DexterityPower:diff()}点敏捷。` |
| `{WeakPower}` | 虚弱 | `给予{WeakPower:diff()}层虚弱。` |
| `{VulnerablePower}` | 易伤 | `给予{VulnerablePower:diff()}层易伤。` |
| `{PoisonPower}` | 中毒 | `给予{PoisonPower:diff()}层中毒。` |
| `{DoomPower}` | 灾厄 | `给予{DoomPower:diff()}层灾厄。` |

**PowerVar 源码**：

```csharp
public class PowerVar<T> : DynamicVar where T : PowerModel
{
    public PowerVar(decimal powerAmount)
        : base(typeof(T).Name, powerAmount)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        if (runGlobalHooks)
        {
            base.PreviewValue = Hook.ModifyPowerAmountGiven(card.CombatState, ModelDb.Power<T>(), card.Owner.Creature, base.BaseValue, target, card, out IEnumerable<AbstractModel> _);
        }
    }
}
```

### 计算变量

计算变量用于动态计算伤害或格挡值，考虑各种加成修正。

| 名称 | 对应类 | 说明 | 示例 |
| --- | --- | --- | --- |
| `{CalculatedDamage}` | `CalculatedDamageVar` | 计算出的伤害量 | `（造成{CalculatedDamage:diff()}点伤害）` |
| `{CalculatedBlock}` | `CalculatedBlockVar` | 计算出的格挡值 | `（获得{CalculatedBlock:diff()}点格挡）` |

**CalculatedVar 基类**：

```csharp
public class CalculatedVar : DynamicVar
{
    private Func<CardModel, Creature?, decimal>? _multiplierCalc;

    public decimal Calculate(Creature? target)
    {
        CardModel cardModel = (CardModel)_owner;
        decimal multiplier = _multiplierCalc(cardModel, target);
        return GetBaseVar().BaseValue + GetExtraVar().BaseValue * multiplier;
    }
}
```

### 特殊变量

| 名称 | 对应类 | 说明 |
| --- | --- | --- |
| `{IfUpgraded}` | `IfUpgradedVar` | 根据升级状态显示不同内容 |
| `{BoolVar}` | `BoolVar` | 布尔值变量 |
| `{StringVar}` | `StringVar` | 字符串变量 |
| `{IntVar}` | `IntVar` | 整数变量 |

**IfUpgradedVar 使用**：

```csharp
public class IfUpgradedVar : DynamicVar
{
    public const string defaultName = "IfUpgraded";
    public UpgradeDisplay upgradeDisplay;
}

public enum UpgradeDisplay
{
    Normal,
    Upgraded,
    UpgradePreview
}
```

## Formatter 格式化器

用于格式化变量的表现形式，使用 `SmartFormat` 库。例如 `{Energy:energyIcons()}` 表示展示 n 个能量图标，n 为 `Energy` 的数值。

格式化器在 `LocManager.LoadLocFormatters()` 中注册：

```csharp
_smartFormatter.AddExtensions(
    new PluralLocalizationFormatter(),
    new ConditionalFormatter(),
    new ChooseFormatter(),
    new LocaleNumberFormatter(),
    new DefaultFormatter(),
    new AbsoluteValueFormatter(),
    new EnergyIconsFormatter(),
    new StarIconsFormatter(),
    new HighlightDifferencesFormatter(),
    new HighlightDifferencesInverseFormatter(),
    new PercentMoreFormatter(),
    new PercentLessFormatter(),
    new ShowIfUpgradedFormatter()
);
```

### 游戏自定义 Formatter

#### diff() - 差异高亮

高于基础变绿，低于基础变红。用于战斗或升级预览。

**源码实现**：

```csharp
public class HighlightDifferencesFormatter : IFormatter
{
    public string Name => "diff";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        if (!(formattingInfo.CurrentValue is DynamicVar dynamicVar))
            return false;
        
        formattingInfo.Write(dynamicVar.ToHighlightedString(inverse: false));
        return true;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-STRIKE.description": "造成 {Damage:diff()} 点伤害。"
}
```

#### inverseDiff() - 反向差异高亮

高于基础变红，低于基础变绿。适用于"失去生命"等负面效果。

**使用示例**：

```json
{
  "MYMOD-SACRIFICE.description": "失去 {HpLoss:inverseDiff()} 点生命。"
}
```

#### energyIcons() - 能量图标

把数值渲染成能量图标。

**源码实现**：

```csharp
public class EnergyIconsFormatter : IFormatter
{
    public string Name => "energyIcons";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        int count = Convert.ToInt32(dynamicVar.PreviewValue);
        string iconPath = $"[img]res://images/packed/sprite_fonts/{colorPrefix}_energy_icon.png[/img]";
        
        // 1-3 显示图标，其他显示数字+图标
        string result = (count <= 0 || count >= 4) 
            ? $"{count}{iconPath}" 
            : string.Concat(Enumerable.Repeat(iconPath, count));
        
        formattingInfo.Write(result);
        return true;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-GAIN_ENERGY.description": "获得 {Energy:energyIcons()}。"
}
```

#### starIcons() - 辉星图标

把数值渲染为辉星图标。

**源码实现**：

```csharp
public class StarIconsFormatter : IFormatter
{
    public string Name => "starIcons";
    private const string _starIconPath = "res://images/packed/sprite_fonts/star_icon.png";
    public const string starIconSprite = "[img]res://images/packed/sprite_fonts/star_icon.png[/img]";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        int count = (int)dynamicVar.PreviewValue;
        string result = string.Concat(Enumerable.Repeat(starIconSprite, count));
        formattingInfo.Write(result);
        return true;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-GAIN_STARS.description": "获得 {Stars:starIcons()}。"
}
```

#### show - 升级状态显示

根据升级情况显示不同文本，与 `IfUpgradedVar` 配合使用。

**源码实现**：

```csharp
public class ShowIfUpgradedFormatter : IFormatter
{
    public string Name => "show";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        if (!(formattingInfo.CurrentValue is IfUpgradedVar ifUpgradedVar))
            return false;

        IList<Format> options = formattingInfo.Format?.Split('|');
        Format upgradedText = options[0];
        Format normalText = options.Count > 1 ? options[1] : null;

        switch (ifUpgradedVar.upgradeDisplay)
        {
            case UpgradeDisplay.Normal:
                formattingInfo.FormatAsChild(normalText, formattingInfo.CurrentValue);
                break;
            case UpgradeDisplay.Upgraded:
                formattingInfo.FormatAsChild(upgradedText, formattingInfo.CurrentValue);
                break;
            case UpgradeDisplay.UpgradePreview:
                formattingInfo.Write("[green]");
                formattingInfo.FormatAsChild(upgradedText, formattingInfo.CurrentValue);
                formattingInfo.Write("[/green]");
                break;
        }
        return true;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-UPGRADE_CARD.description": "[gold]升级[/gold]你[gold]手牌[/gold]中的{IfUpgraded:show:所有牌|一张牌}。"
}
```

#### abs - 绝对值

显示数值的绝对值。

**源码实现**：

```csharp
public class AbsoluteValueFormatter : IFormatter
{
    public string Name => "abs";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        object value = formattingInfo.CurrentValue;
        string result = value switch
        {
            decimal v => Math.Abs(v).ToString(Culture),
            double v => Math.Abs(v).ToString(Culture),
            int v => Math.Abs(v).ToString(),
            _ => null
        };
        if (result != null)
            formattingInfo.Write(result);
        return result != null;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-ABS_DAMAGE.description": "造成 {Damage:abs()} 点伤害。"
}
```

#### percentMore() / percentLess() - 百分比

将倍率转换为百分比显示。

**源码实现**：

```csharp
public class PercentMoreFormatter : IFormatter
{
    public string Name => "percentMore";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        decimal value = formattingInfo.CurrentValue is DynamicVar dv 
            ? dv.BaseValue 
            : Convert.ToDecimal(formattingInfo.CurrentValue);
        
        // 1.5 -> 50%, 2.0 -> 100%
        formattingInfo.Write(Convert.ToInt32((value - 1m) * 100m).ToString());
        return true;
    }
}

public class PercentLessFormatter : IFormatter
{
    public string Name => "percentLess";

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        decimal value = formattingInfo.CurrentValue is DynamicVar dv 
            ? dv.BaseValue 
            : Convert.ToDecimal(formattingInfo.CurrentValue);
        
        // 0.5 -> 50%, 0.75 -> 25%
        formattingInfo.Write(Convert.ToInt32((1m - value) * 100m).ToString());
        return true;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-BOOST_DAMAGE.description": "额外造成 {Boost:percentMore()}% 伤害。",
  "MYMOD-REDUCE_DAMAGE.description": "受到的伤害减少 {Reduction:percentLess()}%。"
}
```

#### n - 本地化数字格式

使用当前语言环境的数字格式。

**源码实现**：

```csharp
public class LocaleNumberFormatter : IFormatter
{
    public string Name => "n";
    public bool CanAutoDetect { get; set; } = true;

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        string format = string.IsNullOrEmpty(formattingInfo.Format?.RawText) 
            ? "N0" 
            : formattingInfo.Format.RawText;
        
        string result = ((IFormattable)formattingInfo.CurrentValue).ToString(format, Culture);
        formattingInfo.Write(result);
        return true;
    }
}
```

**使用示例**：

```json
{
  "MYMOD-LARGE_NUMBER.description": "造成 {Damage:n} 点伤害。",
  "MYMOD-DECIMAL.description": "精确值：{Value:n2}"
}
```

### SmartFormat 内置 Formatter

详细参考 [SmartFormat Wiki](https://github.com/axuno/SmartFormat/wiki)。

#### cond - 条件分支

根据条件显示不同内容。

**语法**：`{Variable:cond:条件?真值|假值}`

**使用示例**：

```json
{
  "MYMOD-FAN_OF_KNIVES.description": "{FanOfKnivesAmount:cond:>0? 对所有敌人|}造成{Damage:diff()}点伤害。",
  "MYMOD-CONDITIONAL.description": "{Count:cond:==0?没有|有{Count:diff()}个}敌人。"
}
```

#### choose - 选择分支

按索引或值选择分支。

**语法**：`{Variable:choose(值1|值2|...):选项1|选项2|...|默认选项}`

**使用示例**：

```json
{
  "MYMOD-SKILL_REPEAT.description": "你打出的下{Skills:choose(1):一|{:diff()}}张技能牌会被额外打出一次。",
  "MYMOD-TIER.description": "等级：{Tier:choose(1|2|3):低|中|高|未知}"
}
```

#### plural - 复数

根据数量选择单复数形式（主要用于英语环境）。

**使用示例**：

```json
{
  "MYMOD-DRAW_CARDS.description": "Draw {Cards:diff()} {Cards:plural:card|cards}."
}
```

#### list - 列表拼接

拼接列表元素。

**语法**：`{List:list:{item}|分隔符|最后分隔符}`

**使用示例**：

```json
{
  "MYMOD-ENEMY_LIST.description": "敌人：{Enemies:list:{}|, |和}"
}
```

## 卡牌独有上下文变量

卡牌有一些额外的上下文变量，由游戏引擎自动提供：

| 名称 | 含义 | 典型写法 |
| --- | --- | --- |
| `singleStarIcon` | 星星图标 | `每获得{singleStarIcon}时` |
| `InCombat` | 是否处于战斗 | `{InCombat:\n（命中{CalculatedHits:diff()}次）|}` |
| `IsTargeting` | 当前是否有目标 | `{IsTargeting:\n（造成{CalculatedDamage:diff()}）|}` |
| `OnTable` | 牌是否在手牌或出牌区 | `{OnTable:cond:true?在场上|不在场上}` |
| `IfUpgraded` | 是否升级 | `[gold]升级[/gold]你[gold]手牌[/gold]中的{IfUpgraded:show:所有牌|一张牌}。` |

### 上下文变量使用场景

**战斗中显示额外信息**：

```json
{
  "MYMOD-COMBAT_CARD.description": "造成 {Damage:diff()} 点伤害。{InCombat:\n（命中{CalculatedHits:diff()}次）|}"
}
```

**目标相关显示**：

```json
{
  "MYMOD-TARGET_CARD.description": "{IsTargeting:\n（造成{CalculatedDamage:diff()}点伤害）|}选择一个目标。"
}
```

## 使用示例

### 基础卡牌描述

```json
{
  "MYMOD-STRIKE.title": "打击",
  "MYMOD-STRIKE.description": "造成 {Damage:diff()} 点伤害。"
}
```

### 带条件的描述

```json
{
  "MYMOD-FAN_OF_KNIVES.title": "飞刀扇",
  "MYMOD-FAN_OF_KNIVES.description": "{FanOfKnivesAmount:cond:>0? 对所有敌人|}造成 {Damage:diff()} 点伤害。"
}
```

### 升级相关描述

```json
{
  "MYMOD-UPGRADE_CARD.title": "升级",
  "MYMOD-UPGRADE_CARD.description": "[gold]升级[/gold]你[gold]手牌[/gold]中的{IfUpgraded:show:所有牌|一张牌}。"
}
```

### 多效果卡牌

```json
{
  "MYMOD-COMPLEX_CARD.title": "复杂卡牌",
  "MYMOD-COMPLEX_CARD.description": "造成 {Damage:diff()} 点伤害。\n获得 {Block:diff()} 点格挡。\n{Heal:cond:>0?回复 {Heal:diff()} 点生命。|}"
}
```

### 能力卡牌

```json
{
  "MYMOD-STRENGTH_CARD.title": "力量注入",
  "MYMOD-STRENGTH_CARD.description": "获得 {StrengthPower:diff()} 点力量。"
}
```

### 遗物描述

```json
{
  "MYMOD-RELIC.title": "神秘遗物",
  "MYMOD-RELIC.description": "战斗开始时，获得 {Block:diff()} 点格挡。",
  "MYMOD-RELIC.flavor": "一个古老的护身符，散发着神秘的光芒。"
}
```

### 能力描述

```json
{
  "MYMOD-CUSTOM_POWER.title": "自定义能力",
  "MYMOD-CUSTOM_POWER.description": "回合开始时，获得 {amount} 点力量。",
  "MYMOD-CUSTOM_POWER.smartDescription": "回合开始时，获得 {amount} 点力量。"
}
```

## 注意事项

### BBCode 标签嵌套

不支持交叉嵌套，应使用正确的嵌套顺序：

- ❌ 错误：`[b]bold[i]bold italic[/b]italic[/i]`
- ✅ 正确：`[b]bold[i]bold italic[/i][/b][i]italic[/i]`

### 空格处理

与 HTML 不同，`RichTextLabel` 不会移除首尾空格，连续空格也会原样显示。

### 用户输入安全

处理用户输入时，使用 `[lb]` 和 `[rb]` 转义方括号，防止 BBCode 注入：

```csharp
string safeText = userInput.Replace("[", "[lb]").Replace("]", "[rb]");
```

### 本地化键格式

- 卡牌：`{ModId}-{CardId}.title` / `.description` / `.selectionScreenPrompt`
- 能力：`{ModId}-{PowerId}.title` / `.description` / `.smartDescription`
- 遗物：`{ModId}-{RelicId}.title` / `.description` / `.flavor` / `.additionalRestSiteHealText`
- 先古之民：`{ModId}-{AncientId}.title` / `.epithet` / `.pages.{PageName}.description`

### 性能考虑

- 避免在描述中使用过于复杂的嵌套条件
- 大量图标（如 `energyIcons`）会影响渲染性能
- 缓存格式化结果，避免重复计算

## 源代码参考

- `DynamicVar` 基类：`MegaCrit.Sts2.Core.Localization.DynamicVars/DynamicVar.cs`
- Formatter 实现：`MegaCrit.Sts2.Core.Localization.Formatters/`
- LocManager：`MegaCrit.Sts2.Core.Localization/LocManager.cs`
- SmartFormat 库：[https://github.com/axuno/SmartFormat](https://github.com/axuno/SmartFormat)
