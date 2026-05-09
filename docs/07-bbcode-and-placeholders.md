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

使用能力名称作为占位符：

```json
{
  "YUWANCARD-PIG_DOUBT.description": "每回合获得 {PigDoubtPower:diff()} 个随机能力。"
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
| `diff()` | 显示升级差异 | `{Damage:diff()}` |
| `D` | 整数格式 | `{Damage:D}` |
| `F1` | 一位小数 | `{Damage:F1}` |
| `F2` | 两位小数 | `{Damage:F2}` |
| `P0` | 百分比（无小数） | `{Chance:P0}` |
| `P1` | 百分比（一位小数） | `{Chance:P1}` |

### 自定义格式化器

在自定义 `DynamicVar` 中实现：

```csharp
public override string FormatValue(decimal value, string? format = null)
{
    return format switch
    {
        "percent" => $"{value * 100}%",
        "time" => $"{value}次",
        "chance" => $"{value:P0}",
        _ => value.ToString()
    };
}
```

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
  
  "YUWANCARD-PIG_SLEEP.title": "猪猪睡觉",
  "YUWANCARD-PIG_SLEEP.description": "结束你的回合。\n获得 {Block:diff()} 点 [gold]格挡[/gold]。\n恢复 {Heal:diff()} 点生命。"
}
```

### 能力本地化 (powers.json)

```json
{
  "YUWANCARD-PIG_DOUBT_POWER.title": "猪猪怀疑",
  "YUWANCARD-PIG_DOUBT_POWER.description": "每回合开始时，获得 {PigDoubtPower:diff()} 个随机 [gold]能力[/gold]。",
  "YUWANCARD-PIG_DOUBT_POWER.smartDescription": "每回合获得随机能力。"
}
```

### 遗物本地化 (relics.json)

```json
{
  "YUWANCARD-PIG_CARROT.title": "猪猪胡萝卜",
  "YUWANCARD-PIG_CARROT.description": "每回合开始时，获得 1 点 [red]力量[/red]。",
  "YUWANCARD-PIG_CARROT.flavor": "一根新鲜的胡萝卜，猪猪很喜欢。"
}
```

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
4. **使用 smartDescription**：为能力提供简短描述，用于能力栏显示

```json
{
  "YUWANCARD-MY_POWER.title": "我的能力",
  "YUWANCARD-MY_POWER.description": "详细描述：每回合开始时，获得 {MyPower:diff()} 点 [red]力量[/red]。",
  "YUWANCARD-MY_POWER.smartDescription": "每回合获得力量。"
}
```
