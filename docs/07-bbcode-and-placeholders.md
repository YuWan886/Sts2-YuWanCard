# BBCode 与占位符

## BBCode 标签

游戏使用 BBCode 格式来富化文本显示。

### 颜色标签

```json
{
  "description": "获得 [gold]5[/gold] 点能量。"
}
```

**内置颜色**：

| 标签 | 颜色 | 用途 |
|------|------|------|
| `[gold]` | 金色 | 能量、金币 |
| `[red]` | 红色 | 伤害、负面效果 |
| `[green]` | 绿色 | 正面效果、治疗 |
| `[blue]` | 蓝色 | 特殊效果 |
| `[purple]` | 紫色 | 稀有内容 |
| `[white]` | 白色 | 普通文本 |
| `[gray]` | 灰色 | 次要信息 |

### 效果标签

| 标签 | 效果 |
|------|------|
| `[b]` | 粗体 |
| `[i]` | 斜体 |
| `[u]` | 下划线 |
| `[s]` | 删除线 |

### 组合使用

```json
{
  "description": "造成 [red]{Damage}[/red] 点伤害，获得 [green]{Block}[/green] 点 [b]格挡[/b]。"
}
```

---

## 占位符系统

占位符用于在本地化文本中插入动态数值。

### 基础语法

```
{变量名}
{变量名:格式}
```

### 标准占位符

| 占位符 | 说明 |
|--------|------|
| `{Damage}` | 伤害值 |
| `{Block}` | 格挡值 |
| `{Heal}` | 治疗值 |
| `{Energy}` | 能量值 |
| `{Cards}` | 卡牌数量 |
| `{Repeat}` | 重复次数 |

### diff() 格式化

`diff()` 用于显示升级后的数值变化：

```json
{
  "YUWANCARD-PIG_STRIKE.description": "造成 {Damage:diff()} 点伤害。"
}
```

**效果**：
- 未升级：造成 6 点伤害。
- 升级后：造成 **9** 点伤害。（变化部分高亮）

### 能力占位符

能力使用自定义变量名作为占位符。**注意**：动态变量只能在 `smartDescription` 中使用，不能在 `description` 中使用（`description` 用于图鉴显示，不会自动注入 DynamicVar）：

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.description": "每回合获得1个随机的[gold]能力[/gold]。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "每回合获得{PigDoubtPower}个随机的[gold]能力[/gold]。"
}
```

**卡牌的 description 不同**：卡牌 description 会自动注入 DynamicVar，所以可以直接使用：
```json
{
  "YUWANCARD-PIG_STRIKE.description": "造成 {Damage:diff()} 点伤害。"
}
```

### 自定义变量占位符

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => 
    [new DynamicVar("MyValue", 5m)];
```

```json
{
  "description": "触发 {MyValue} 次效果。"
}
```

### 升级条件占位符

使用 `{IfUpgraded:show:...|...}` 显示升级前后的不同文本：

```json
{
  "YUWANCARD-MY_CARD.description": "造成 {Damage} 点伤害。{IfUpgraded:show:\n固有。|}"
}
```

**效果**：
- 未升级：造成 6 点伤害。
- 升级后：造成 9 点伤害。
固有。

---

## 格式化器

### 内置格式化器

| 格式化器 | 说明 | 示例 |
|----------|------|------|
| `diff()` | 显示升级差异（绿色高亮） | `{Damage:diff()}` |
| `energyIcons()` | 显示为能量图标 | `{Energy:energyIcons()}` |
| `D` | 整数格式 | `{Damage:D}` |
| `F1` | 一位小数 | `{Damage:F1}` |
| `F2` | 两位小数 | `{Damage:F2}` |
| `P0` | 百分比（无小数） | `{Chance:P0}` |
| `P1` | 百分比（一位小数） | `{Chance:P1}` |

### 自定义格式化器

内置格式化器（`diff()`、`energyIcons()`、`D`、`F1`、`P0` 等）由游戏的 SmartFormat 扩展提供。自定义格式化可通过 SmartFormat 的 `IFormatter` 接口注册，DynamicVar 本身通过 `IConvertible` 接口参与格式化。

常用的格式化方式是通过给变量起一个好名字并在本地化中使用：
```json
{ "description": "造成 {Damage:diff()} 点伤害。获得 {Energy:energyIcons()}。" }
```

---

## 隐式变量（自动注入）

### 卡牌 description 隐式变量

卡牌的 description 在格式化时会自动注入以下变量，无需在 `CanonicalVars` 中定义：

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `{OnTable}` | bool | 卡牌是否在桌面上（手牌/打出区） |
| `{InCombat}` | bool | 是否在战斗中 |
| `{IsTargeting}` | bool | 是否正在选择目标 |
| `{TargetType}` | string | 目标类型字符串 |
| `{energyPrefix}` | string | 能量图标前缀（用于格式化 EnergyVar） |
| `{singleStarIcon}` | string | 星星图标 img 标签 |
| `{IfUpgraded}` | IfUpgradedVar | 升级条件显示（自动注入，格式 `{IfUpgraded:show:升级文本\|未升级文本}`） |

### 能力 smartDescription 隐式变量

能力的 smartDescription 在格式化时会自动注入以下变量：

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `{Amount}` | int | 当前能力层数 |
| `{Duration}` | int | 持续时间（Duration 类型时） |
| `{OnPlayer}` | bool | 拥有者是否为玩家 |
| `{IsMultiplayer}` | bool | 是否为多人游戏 |
| `{PlayerCount}` | int | 玩家数量 |
| `{OwnerName}` | string | 拥有者名称（角色名或怪物名） |
| `{ApplierName}` | string | 施加者名称（可能为空） |
| `{TargetName}` | string | 目标名称（可能为空） |
| `{singleStarIcon}` | string | 星星图标 img 标签 |
| `{energyPrefix}` | string | 能量图标前缀 |

---

## 本地化文件示例

### 卡牌本地化 (cards.json)

```json
{
  "YUWANCARD-PIG_STRIKE.title": "猪猪打击",
  "YUWANCARD-PIG_STRIKE.description": "造成 {Damage:diff()} 点伤害。",
  
  "YUWANCARD-PIG_DEFEND.title": "猪猪防御",
  "YUWANCARD-PIG_DEFEND.description": "获得 {Block:diff()} 点 [gold]格挡[/gold]。",
  
  "YUWANCARD-PIG_BASH.title": "猪猪重击",
  "YUWANCARD-PIG_BASH.description": "造成 {Damage:diff()} 点伤害。\n目标获得 {Vulnerable:diff()} 层 [red]易伤[/red]。",
  
  "YUWANCARD-GIVE_YOU.title": "我给你",
  "YUWANCARD-GIVE_YOU.description": "选择一张手牌交给目标队友。",
  "YUWANCARD-GIVE_YOU.selectionScreenPrompt": "选择1张手牌交给队友"
}
```

**卡牌本地化键**：
| 键 | 必需 | 说明 |
|-----|------|------|
| `.title` | 是 | 卡牌名称 |
| `.description` | 是 | 卡牌描述（支持动态变量） |
| `.selectionScreenPrompt` | 需要选牌时 | 选牌界面提示文本 |

### 能力本地化 (powers.json)

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪猪怀疑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "每回合开始时，获得1个随机 [gold]能力[/gold]。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "每回合获得{PigDoubtPower}个随机的[gold]能力[/gold]。",
  
  "YUWANCARD-PIG_BRAIN_OVERLOAD_POWER.title": "猪脑过载",
  "YUWANCARD-PIG_BRAIN_OVERLOAD_POWER.description": "每2回合获得1张[gold]晕眩[/gold]。升级后改为每3回合。",
  "YUWANCARD-PIG_BRAIN_OVERLOAD_POWER.smartDescription": "每{DazedInterval}回合获得{Amount}张[gold]晕眩[/gold]。",
  
  "YUWANCARD-CHEF_PIG_POWER.selectionScreenPrompt": "选择1张手牌变化为的食物猪卡牌"
}
```

**能力本地化键**：
| 键 | 必需 | 说明 |
|-----|------|------|
| `.title` | 是 | 能力名称 |
| `.description` | 是 | 能力描述（静态文本，图鉴显示） |
| `.smartDescription` | 推荐 | 智能描述（支持动态变量，战斗悬浮提示） |
| `.remoteDescription` | 可选 | 多人游戏中其他玩家施加时的描述 |
| `.selectionScreenPrompt` | 需要选牌/选目标时 | 选择界面提示文本 |

### 遗物本地化 (relics.json)

```json
{
  "YUWANCARD-PIG_CARROT.title": "猪猪胡萝卜",
  "YUWANCARD-PIG_CARROT.description": "每回合开始时，获得 1 点 [red]力量[/red]。",
  "YUWANCARD-PIG_CARROT.flavor": "一根新鲜的胡萝卜，猪猪很喜欢。"
}
```

**遗物本地化键**：
| 键 | 必需 | 说明 |
|-----|------|------|
| `.title` | 是 | 遗物名称 |
| `.description` | 是 | 遗物效果描述 |
| `.flavor` | 否 | 风味文本（非功能性描述） |

### 修改器本地化 (modifiers.json)

```json
{
  "YUWANCARD-ENDLESS.title": "无尽模式",
  "YUWANCARD-ENDLESS.description": "击败所有章节后重新开始，敌人变得更强。",
  "YUWANCARD-ENDLESS.neow_title": "无尽模式",
  "YUWANCARD-ENDLESS.neow_description": "开始无尽循环，获得 [gold]10[/gold] 点最大生命值。"
}
```

### 事件本地化 (events.json)

```json
{
  "YUWANCARD-PIG_EVENT.pages.page_1.description": "你遇到了一只友好的猪猪。\n它看起来想给你一些东西。",
  "YUWANCARD-PIG_EVENT.pages.page_1.options.option_1.title": "接受礼物",
  "YUWANCARD-PIG_EVENT.pages.page_1.options.option_2.title": "离开"
}
```

### 其他本地化表

| 文件 | 用途 | 键格式示例 |
|------|------|-----------|
| `ancients.json` | 先古之民对话/标题 | `YUWANCARD-{ID}.title`, `.talk.{type}.{idx}.ancient` |
| `monsters.json` | 怪物名称/描述 | `YUWANCARD-{ID}.title`, `.description` |
| `enchantments.json` | 附魔名称/描述 | `YUWANCARD-{ID}.title`, `.description` |
| `orbs.json` | 充能球名称/描述 | `YUWANCARD-{ID}.title`, `.description` |
| `potions.json` | 药水名称/描述 | `YUWANCARD-{ID}.title`, `.description`, `.selectionScreenPrompt` |
| `characters.json` | 角色名称/描述 | `YUWANCARD-{ID}.title`, `.description` |
| `badges.json` | 徽章名称/描述 | `YUWANCARD-{ID}.title`, `.description` |
| `encounters.json` | 遭遇名称 | `YUWANCARD-{ID}.title` |
| `rest_site_ui.json` | 休息站选项 | `OPTION_{ID}.title`, `.description` |
| `card_reward_ui.json` | 卡牌奖励 UI | UI 相关文本 |
| `gameplay_ui.json` | 游戏 UI | UI 相关文本 |
| `settings_ui.json` | 设置 UI | 设置界面文本 |

---

## 特殊字符

### 换行

使用 `\n` 换行：

```json
{
  "description": "第一行。\n第二行。"
}
```

### 转义

使用 `[]` 包含字面量方括号：

```json
{
  "description": "使用 [[gold]] 标签显示金色文本。"
}
```

---

## 最佳实践

1. **使用 diff() 显示升级变化**：让玩家清楚看到升级效果
2. **使用颜色标签突出关键信息**：伤害用红色，治疗用绿色，能量用金色
3. **保持描述简洁**：避免过长的描述文本
4. **使用 smartDescription**：为能力提供包含动态变量的智能描述，用于战斗中悬浮提示
5. **能力 description 用静态文本，smartDescription 用动态变量**：图鉴/卡牌预览中的 description 不会自动注入 DynamicVar，只有 smartDescription 会
6. **卡牌 description 可直接使用动态变量**：卡牌的 description 会自动调用 `DynamicVars.AddTo()`

```json
{
  "YUWANCARD-MY_POWER.title": "我的能力",
  "YUWANCARD-MY_POWER.description": "每回合开始时获得2点力量。（静态文本，图鉴显示）",
  "YUWANCARD-MY_POWER.smartDescription": "每回合获得{MyPower:diff()}点[gold]力量[/gold]。（动态，悬浮提示）"
}
```
