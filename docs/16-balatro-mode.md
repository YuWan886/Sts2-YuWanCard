# Balatro 模式（未完成）

## 概述

Balatro 模式是 YuWanCard 模组的一个自定义**修改器（Modifier）**，ID 为 `YUWANCARD-BALATRO`。激活后，游戏会叠加 6 个互相联动的子系统，将《Balatro》的经典机制引入《Slay the Spire 2》：

- **Joker 槽位系统**：独立于普通遗物的 Joker 遗物装备栏（含背包管理界面）
- **连击乘数系统**：基于回合内打出的卡牌累积连击，伤害获得倍率加成
- **卡牌修饰器系统**：独立于升级/附魔的第三层卡牌强化（铝箔/全息/多彩/负片），含着色器视觉特效
- **加工站系统**：商店内专属标签页，使用金币或修饰器代币为手牌添加修饰器
- **利息经济系统**：每进入新楼层获得当前金币 10% 的利息
- **Balatro 主题卡牌**：13 张消耗品卡牌（塔罗式/星球式/光谱式），加入共享无色卡池

所有系统通过**连击计数器**和**金币经济**互相联动，形成「打牌累积连击 → 连击提升伤害 → 金币/利息支撑卡牌强化 → Joker 提供被动加成」的循环。

---

## 目录

1. [激活方式](#1-激活方式)
2. [Joker 槽位系统](#2-joker-槽位系统)
3. [连击乘数系统](#3-连击乘数系统)
4. [卡牌修饰器系统](#4-卡牌修饰器系统)
5. [加工站系统](#5-加工站系统)
6. [利息经济系统](#6-利息经济系统)
7. [Balatro 卡牌](#7-balatro-卡牌)
8. [Joker 遗物](#8-joker-遗物)
9. [配套遗物](#9-配套遗物)
10. [Balatro 能力](#10-balatro-能力)
11. [UI 与交互](#11-ui-与交互)
12. [文件结构](#12-文件结构)
13. [设计文档对照](#13-设计文档对照)

---

## 1. 激活方式

### 角色选择界面

在角色选择界面，选择**标准模式**后，Ascension 面板右侧会出现 **「Balatro Mode」** 勾选框。勾选后，本次游戏激活 Balatro 修改器。

- 仅在 `GameMode.Standard` 下可见
- 多人游戏中，Host 可切换，Client 只读（通过 `SavedProperty` 自动同步）
- 不可用于每日挑战（`AllowedInDailyRun = false`）
- 不可用于自定义模式（`AllowedInCustomRun = false`）

### 代码检测

```csharp
// 检查当前 RunState 是否激活了 Balatro 修改器
bool active = BalatroModifier.IsActive(runState);

// 获取修改器实例
BalatroModifier? modifier = BalatroModifier.GetInstance(runState);
```

---

## 2. Joker 槽位系统

### 2.1 槽位机制

Joker 遗物**不占用**普通遗物槽，拥有自己独立的装备栏：

| 槽位 | 解锁条件 | 说明 |
|------|---------|------|
| 1 | 初始解锁 | 修改器激活即可用 |
| 2 | 初始解锁 | 修改器激活即可用 |
| 3 | 初始解锁 | 修改器激活即可用 |
| 4 | 击杀第 1 个 Boss | 完成第一幕后自动解锁（`AfterCombatVictory`，act ≤ 1） |
| 5 | 击杀第 2 个 Boss | 完成第二幕后自动解锁（`AfterCombatVictory`，act ≤ 2） |
| 6 | 获得「负片小丑」 | 仅在拥有 NegativeJoker 时生效，同时解锁第 4、5 槽（若未解锁） |

### 2.2 Joker 获取方式

- **精英掉落**：击杀精英有 25% 概率额外掉落一个 Joker 遗物（独立于普通遗物奖励，不重复获得已拥有的 Joker）
- **Boss 掉落**：击杀 Boss 有 100% 概率额外掉落一个 Joker 遗物
- **修饰器代币掉落**：击杀精英/Boss 必定额外掉落一个 `ModifierToken` 遗物（用于加工站）
- **空白兑换券**：BlankVoucher 遗物获得时从 3 个随机 Joker 中选择 1 个获得

### 2.3 Joker 的装备与存储

Joker 遗物继承 `YuWanJokerRelicModel`（位于 `Relics/Jokers/BalatroJokerRelicModel.cs`），核心行为：

1. **获得时**（`AfterObtained`）→ 自动尝试装备到空闲槽位（`AcquireJoker`）
2. 若槽位已满 → 存入 **Joker 背包**（以 `|` 分隔的字符串列表持久化到 `YUWANCARD_JokerBag`）
3. 装备后 → 从普通遗物栏移除（`RelicCmd.Remove`）
4. 解锁新槽位时 → 自动从背包中取出装备
5. 背包管理 → 通过 `NJokerSlotBar` 和 `NJokerBagPopup` UI 进行装备/卸下操作

### 2.4 Joker 背包

背包是一个持久化的 Joker ID 列表，存储于 `YUWANCARD_JokerBag`（`|` 分隔的字符串 SavedProperty）。支持以下操作：

- `TryEquipBagJoker(jokerId, slotIndex)` — 从背包装备到指定槽位
- `TryUnequipJoker(slotIndex)` — 从槽位卸下到背包
- 背包容量无上限
- 重复 Joker 判定会同时检查装备槽位和背包中的 ID

### 2.5 代码接口

```csharp
// 获取当前可用槽位数（3~6）
int capacity = modifier.GetCurrentJokerCapacity();

// 获取当前装备的 Joker ID 列表（仅已装备的，不含背包）
IReadOnlyList<string> equipped = modifier.GetEquippedJokerIds();

// 获取全部槽位 ID（含空槽位）
IReadOnlyList<string> allSlots = modifier.GetAllJokerSlotIds();

// 检查槽位是否解锁
bool unlocked = modifier.IsJokerSlotUnlocked(slotIndex);

// 获取 Joker 元数据（从 ModelDb 查询）
string title = modifier.GetJokerTitle(jokerId);
string description = modifier.GetJokerDescription(jokerId);
Texture2D? icon = modifier.GetJokerIcon(jokerId);

// 背包操作
IReadOnlyList<string> bag = modifier.GetJokerBagIds();
bool equipped = modifier.TryEquipBagJoker(jokerId, slotIndex);
bool unequipped = modifier.TryUnequipJoker(slotIndex);
```

---

## 3. 连击乘数系统

### 3.1 核心规则

- **累积**：每打出一张牌（不含状态/诅咒，除非有 WildCard 遗物），获得连击
- **乘数公式**：`乘数 = 1 + 连击数 × 0.1 + 传奇小丑加成`。连击上限 30（乘数上限 ≥ ×4.0）
- **传奇小丑加成**：`LegendBonus = CardsPlayedThisTurn × 0.2 × LegendJokerCount`
- **应用**：所有攻击牌的最终伤害 × 当前乘数（`ModifyDamageMultiplicative`）
- **重置**：回合结束时连击归零
- **跨回合保留**：连击 ≥ 20 时获得 1 层**惯性**能力，下回合保留 10% 连击

### 3.2 连击加成计算

| 卡牌条件 | 连击加成 | 备注 |
|---------|---------|------|
| 基础牌 | +1.0 | 所有非特殊类型卡牌 |
| 罕见牌 | +1.5 | 额外 +0.5 |
| 稀有/Ancient 牌 | +2.0 | 额外 +1.0 |
| 带修饰器的牌 | 额外 +1.0 | 铝箔/全息/多彩/负片 |
| 连续打出同类型 | 额外 +1.0 | 攻击→攻击、技能→技能（`LastCardTypeThisTurn` 追踪） |
| 0 费牌 | 额外 +0.5 | 不含 X 费 |
| 状态牌（需 WildCard） | +0.5 | 仅在有 WildCard 遗物时 |
| 诅咒牌（需 WildCard） | +2.0 | 仅在有 WildCard 遗物时 |

### 3.3 跨回合保留

| 条件 | 保留比例 | 来源 |
|------|---------|------|
| 连击 ≥ 20 | 10% | 自动获得 InertiaPower（1 层，回合开始时自毁） |
| 拥有 SteelJoker | 20%（取最高） | 遗物被动 |

保留计算使用缩放整数避免浮点精度问题：`YUWANCARD_RetainedComboScaled = (int)(ComboCounter × retainRatio × RetainedComboScale)`，下回合恢复时除以 `RetainedComboScale`。

### 3.4 代码接口

```csharp
// 获取当前显示文本
string comboText = modifier.GetComboDisplayText(); // "COMBO 12.0  MULT x2.2"

// 连击乘数（只读）
float multiplier = modifier.ComboMultiplier;

// 当前连击数
float combo = modifier.ComboCounter;

// 本回合已打出的卡牌数
int cardsPlayed = modifier.CardsPlayedThisTurn;
```

---

## 4. 卡牌修饰器系统

### 4.1 概述

独立于升级和附魔的**第三层卡牌强化**。每张 `YuWanCardModel` 卡牌最多拥有 **1 个修饰器**。修饰器数据通过 `YUWANCARD_Edition` 和 `YUWANCARD_FoilApplied` 两个 SavedProperty 持久化。

修饰器系统涉及多个层面协同工作：
- **数据层**：`BalatroCardEdition` 枚举 + CardModel 扩展属性
- **持久化层**：`BalatroCardEditionPersistencePatch` 拦截序列化/反序列化
- **视觉层**：`BalatroCardEditionVisualPatch` 在 NCard 上叠加着色器边框
- **关键词层**：`BalatroCardKeywords` 注册自定义 CardKeyword（Foil/Holographic/Polychrome/Negative）

### 4.2 修饰器类型

| 修饰器 | 枚举值 | 效果 | 视觉风格 | 关键词 |
|--------|--------|------|---------|--------|
| 铝箔 (Foil) | `BalatroCardEdition.Foil` | 所有正数值 +20%（向下取整，最小+1），仅应用一次 | 银色脉冲边框 | `Foil` |
| 全息 (Holographic) | `BalatroCardEdition.Holographic` | 战斗中卡牌费用 -1（最低 0） | 蓝紫渐变边框 | `Holographic` |
| 多彩 (Polychrome) | `BalatroCardEdition.Polychrome` | 打出时卡牌效果额外触发 1 次 | 彩虹流动边框 | `Polychrome` |
| 负片 (Negative) | `BalatroCardEdition.Negative` | 标记为有修饰器（触发联动效果），不计入牌组数量 | 紫色反转边框 | `Negative` |

### 4.3 视觉特效（着色器）

`BalatroCardEditionVisualPatch` 通过 Harmony Postfix 拦截 `NCard.Reload`，在卡牌 Body 节点上叠加一个全屏 `ColorRect`，使用自定义着色器 `balatro_card_edition_border.gdshader` 渲染边框特效：

| 模式 | 着色器参数 | 视觉描述 |
|------|----------|---------|
| mode=0 (Foil) | `#F2F2FF` → `#B8C1D6` 混合 | 银色边框，正弦波脉冲 |
| mode=1 (Holographic) | `#51B8FF` → `#B561FF` 渐变 | 蓝紫横向渐变 |
| mode=2 (Polychrome) | RGB 通道独立正弦波 | 彩虹流动（R/G/B 相位差 2.1/4.2） |
| mode=3 (Negative) | 紫 → 深紫反转脉冲 | 紫色反转，降饱和 |

着色器参数：
- `border_width`: 0.06（边框宽度）
- `pulse_speed`: 1.6（动画速度）
- 边框使用 `smoothstep` 实现柔和边缘
- 中心区域透明（`center_mask` 移除卡片内部渲染）
- 在 `NCard.OnFreedToPool` 时自动清理

### 4.4 获取方式

| 途径 | 说明 |
|------|------|
| 加工站 | 商店内 Balatro 标签页，使用金币或修饰器代币购买修饰器（详见 [加工站系统](#5-加工站系统)） |
| 「黑曜石」卡牌 | 对手牌中一张无修饰器的牌添加多彩修饰器（未升级时有代价：随机消耗牌组中另一张牌） |
| 「虚空」卡牌 | 对手牌中一张无修饰器的牌添加负片修饰器（代价：失去最大生命值的 10%） |

### 4.5 修饰器规则

- **唯一性**：已有修饰器的牌不可再次添加（`CanApplyBalatroEdition` 返回 false）
- **限制**：不可对状态/诅咒/任务/None 类型卡牌添加修饰器
- **保留**：升级、Deserialize、MutableClone 时保留修饰器
  - `BalatroCardEditionPersistencePatch` 拦截 `ToSerializable` → 将修饰器写入 `SerializableCard` 的 generic 数据
  - 拦截 `FromSerializable` → 从 `SerializableCard` 恢复修饰器
  - 拦截 `DowngradeInternal` → 降级后刷新修饰器状态
  - 拦截 `MutableClone` → 复制卡牌时复制修饰器
- **连击加成**：打出带修饰器的卡牌，连击额外 +1
- **多彩联动**：多彩修饰器卡牌额外触发 1 次；若同时拥有 PolychromeJoker，额外触发 2 次（原 1 + Joker 1）
- **全息减费**：在 `TryModifyEnergyCostInCombat` 中实现，减 1 费（最低 0）
- **铝箔加成**：在 `AfterCardStateRebuild` 时应用（`RefreshEditionAfterCardStateRebuild`），确保反序列化后正确应用

### 4.6 代码接口

```csharp
// 卡牌上的属性
BalatroCardEdition edition = card.BalatroEdition;
bool hasEdition = card.HasBalatroEdition;

// 检查是否可以添加修饰器
bool canApply = card.CanApplyBalatroEdition(BalatroCardEdition.Foil);

// 尝试添加修饰器（会自动应用到 DeckVersion）
bool applied = card.TryApplyBalatroEdition(BalatroCardEdition.Polychrome);

// 通过 Helper 安全应用（同时处理 DeckVersion 和 GrowingJoker 联动）
bool result = BalatroCardEditionHelper.TryApplyEdition(card, edition);

// 获取多彩修饰器的额外打出次数
int bonus = card.GetBalatroPlayCountBonus(); // 1 if Polychrome, else 0

// 检查卡牌是否有任意修饰器（Helper 静态方法）
bool hasEdition = BalatroCardEditionHelper.HasEdition(card);

// 获取卡牌修饰器类型（Helper 静态方法）
BalatroCardEdition edition = BalatroCardEditionHelper.GetEdition(card);

// 序列化/反序列化（由 Patch 自动调用）
BalatroCardEditionHelper.WriteGenericEditionToSerializable(card, serializableCard);
BalatroCardEditionHelper.RestoreGenericEditionFromSerializable(card, serializableCard);

// 复制修饰器状态到克隆卡牌
BalatroCardEditionHelper.CopyEditionStateToClone(source, clone);

// 在卡牌状态重建后刷新修饰器效果
BalatroCardEditionHelper.RefreshEditionAfterCardStateRebuild(card);
```

---

## 5. 加工站系统

### 5.1 概述

加工站（Mod Station）是商店界面的第二个标签页，允许玩家使用**金币**或**修饰器代币**为手牌中的卡牌添加修饰器。这是一个完整的商店扩展，通过 `BalatroMerchantPatch` 注入到 `NMerchantInventory` 中。

### 5.2 加工站定价

| 修饰器 | 金币价格 | 修饰器代币 |
|--------|---------|-----------|
| 铝箔 (Foil) | 75 | 1 个代币 |
| 全息 (Holographic) | 75 | 1 个代币 |
| 多彩 (Polychrome) | 150 | 1 个代币 |
| 负片 (Negative) | 250 | 1 个代币 |
| 刷新商品 | 25 | — |

- 每层首次进入商店时自动刷新 2 个加工站商品（`EnsureModStationOffers`）
- 若当前楼层已有有效商品则不刷新（`YUWANCARD_ModStationFloor` 防重复）
- 付费刷新（25 金币）可重新随机商品
- 玩家可消耗 1 个修饰器代币替代金币支付
- 购买后弹出卡牌选择界面，从手牌中选择一张可添加修饰器的牌

### 5.3 修饰器代币

修饰器代币是一种特殊货币，通过击杀精英/Boss 获得：

- **来源**：击杀精英或 Boss 后，`TryModifyRewards` 额外添加一个 `ModifierToken` 遗物奖励
- **存储**：`YUWANCARD_ModifierTokens`（SavedProperty int，持久化）
- **上限**：无上限
- **使用**：在加工站购买时优先消耗代币（1 代币 = 任意修饰器），代币不足时使用金币
- **获取流程**：`ModifierToken` 遗物被拾取 → `AfterObtained` → `modifier.AddModifierTokens(1)` → `RelicCmd.Remove` 自毁
- **注意**：`ModifierToken` 遗物稀有度为 `RelicRarity.None`，不在任何遗物池中，仅通过奖励掉落

### 5.4 购买流程

1. 在商店界面点击「加工站」标签页
2. 查看当前 2 个修饰器商品（对应 6 个 Joker 槽位的装备状态显示在上方）
3. 点击想购买的修饰器卡片
4. 系统检查手牌中是否有可添加该修饰器的卡牌
5. 弹出卡牌选择界面（标准 `CardSelection` 流程）
6. 选择目标卡牌 → 应用修饰器 → 扣除金币或代币 → 刷新 UI
7. 若拥有 GrowingJoker，同时获得 3 最大生命值

### 5.5 代码接口

```csharp
// 获取当前商品列表
IReadOnlyList<BalatroCardEdition> offers = modifier.GetModStationOffers();

// 确保商品已刷新（进入商店时调用）
modifier.EnsureModStationOffers(player);

// 刷新商品（付费/免费）
bool success = await modifier.RefreshModStationOffers(player, payRefreshCost: true);

// 购买商品
bool purchased = await modifier.PurchaseModStationOffer(player, edition);

// 获取修饰器价格
int cost = modifier.GetEditionShopCost(edition);

// 代币操作
int tokens = modifier.ModifierTokenCount;
modifier.AddModifierTokens(1);
```

---

## 6. 利息经济系统

### 6.1 核心规则

| 参数 | 值 | 说明 |
|------|-----|------|
| 利率 | 10% | 当前金币 × 10%，向下取整 |
| 触发时机 | 每进入新楼层 | 含战斗、商店、宝箱、休息点等节点 |
| 上限 | 10 金币/层 | 持有 ≥ 100 金币后封顶 |
| 下限 | 0 | 金币不足 10 时不产生利息 |
| 复利加成 | +5/层 | CompoundInterestPower 每层 +5 利息上限 |
| 银行家加成 | +3/个 | BankerJoker 每个额外 +3 金币 |

### 6.2 实现细节

```
利息计算 = min(floor(gold × 0.1), 10 + compoundInterestCapBonus) + 3 × bankerJokerCount
```

利息在 `AfterRoomEntered` 中触发，使用 `YUWANCARD_LastInterestFloor` 防止同一楼层重复触发。

### 6.3 金币修饰保护

`InflationPower` 使用 `GoldModificationGuard` 防止无限递归——当金币加成事件触发自身时，Guard 检测递归深度并跳过重复触发。`ShouldGainGold` 和 `AfterGoldGained` 代理给 Guard 处理。

---

## 7. Balatro 卡牌

全部 13 张卡牌注册在 `BalatroCardPool`（共享无色卡池，`IsColorless = true`, `IsShared = true`），全角色可获取。

### 7.1 经济型卡牌（5 张）

| 名称 | 费用 | 类型 | 稀有度 | 效果 |
|------|------|------|--------|------|
| **Investment** (投资) | 1 | 技能 | 普通 | 获得 5 金币。升级：8 金币 |
| **Compound Interest** (复利) | 2 | 能力 | 罕见 | 获得 CompoundInterest 能力，利息上限 +5。升级：+10 |
| **Dividend** (分红) | 0 | 技能 | 罕见 | 本回合每 5 连击获得 3 金币。消耗。升级：移除消耗 |
| **Bankruptcy** (破产) | 2 | 攻击 | 稀有 | 花费所有金币，造成花费金币的 50% 伤害。升级：75% |
| **Inflation** (通货膨胀) | 3 | 能力 | 稀有 | 获得金币 +50%，非 X 费卡牌费用 +1。升级：+75% |

### 7.2 塔罗式卡牌（4 张）— 卡牌操控

| 名称 | 费用 | 稀有度 | 效果 |
|------|------|--------|------|
| **Magician** (魔术师) | 0 | 普通 | 选择手牌中一张可转换的牌，转换为同费用的随机牌。消耗 |
| **Priestess** (女祭司) | 0 | 普通 | 选择手牌中一张牌，复制加入抽牌堆。消耗。升级：复制 2 份 |
| **Emperor** (皇帝) | 1 | 罕见 | 选择手牌中一张牌，费用永久设为 0。消耗。升级：额外选 1 张 |
| **Death** (死神) | 1 | 罕见 | 选择手牌中一张牌，从牌组永久删除。获得 15 金币。消耗。升级：25 金币 |

### 7.3 星球式卡牌（2 张）— 威力缩放

| 名称 | 费用 | 稀有度 | 效果 |
|------|------|--------|------|
| **Mercury** (水星) | 1 | 普通 | 本场战斗中所有攻击牌伤害 +1。消耗。升级：+2 |
| **Venus** (金星) | 1 | 普通 | 本场战斗中所有技能牌格挡 +1。消耗。升级：+2 |

### 7.4 光谱式卡牌（2 张）— 高风险回报

| 名称 | 费用 | 稀有度 | 效果 |
|------|------|--------|------|
| **Obsidian** (黑曜石) | 0 | 稀有 | 选择手牌中一张无修饰器的牌，添加多彩修饰器。未升级时随机消耗牌组中另一张牌。消耗 |
| **Void** (虚空) | 0 | 稀有 | 选择手牌中一张无修饰器的牌，添加负片修饰器。失去 10% 最大生命值。消耗。升级：失去 5% |

### 7.5 获取途径

- **卡牌奖励**：Balatro 卡池加入战斗奖励和商店卡牌池（`ModifyCardRewardCreationOptions`）
- **非猪角色限制**：非猪角色不会在卡池中看到 Investment、CompoundInterest、Dividend、Bankruptcy、Inflation（这些卡牌依赖猪的经济机制，通过 `ModifyCardRewardCreationOptions` 中检查角色类型过滤）
- **卡池颜色**：淡金色主题（`ShaderColor: #E8DCC3`, `EnergyOutlineColor: #4A3B2A`）

---

## 8. Joker 遗物

全部 12 个 Joker 遗物继承 `YuWanJokerRelicModel`（位于 `Relics/Jokers/BalatroJokerRelicModel.cs`），注册在 `SharedRelicPool`。它们**不出现在普通遗物池和商店中**（`IsAllowed => false`, `IsAllowedInShops => false`），仅通过精英/Boss 奖励掉落和 BlankVoucher 获取。

| # | 名称 | 稀有度 | 效果 | 联动系统 |
|---|------|--------|------|---------|
| 1 | **GreedJoker** (贪婪小丑) | Common | 每打出第 3 张攻击牌，获得 5 金币 | 连击 × 经济 |
| 2 | **GluttonyJoker** (暴食小丑) | Common | 每打出第 4 张技能牌，回复 3 生命 | 技能连击 |
| 3 | **MirrorJoker** (镜像小丑) | Common | 连续打出同类型牌时，后续牌额外触发 1 次 | 同类型连打 |
| 4 | **MiserJoker** (守财小丑) | Uncommon | 手牌中每有 1 张 0 费牌，攻击牌伤害 +1（加法） | 卡牌属性条件 |
| 5 | **CollectorJoker** (收藏小丑) | Uncommon | 牌组中每 5 张 Rare/Ancient 牌，回合开始获得 1 能量 | 稀有度经济 |
| 6 | **GamblerJoker** (赌徒小丑) | Uncommon | 连击 ≥ 5 时，随机对一名敌人造成 8~20 伤害 | 连击阈值触发 |
| 7 | **PolychromeJoker** (多彩小丑) | Rare | 打出带修饰器的卡牌时，额外触发 1 次 | 修饰器联动 |
| 8 | **NegativeJoker** (负片小丑) | Rare | 解锁第 6 个 Joker 槽位，同时解锁第 4、5 槽 | 槽位扩展 |
| 9 | **LegendJoker** (传奇小丑) | Ancient | 回合内每打 1 张牌，连击乘数额外 +0.2 | 核心输出增幅 |
| 10 | **HolographicJoker** (全息小丑) | Ancient | 回合开始时，复制上回合打出的第一张牌加入手牌 | 跨回合策略 |
| 11 | **BankerJoker** (银行家小丑) | Uncommon | 利息触发时额外获得 3 金币 | 经济增强 |
| 12 | **InvestorJoker** (投资家小丑) | Rare | 商店消费的 20% 返还 | 经济回收 |

### Joker 效果实现详解

- **GreedJoker**：在 `AfterCardPlayed` 中计数攻击牌（`_attackCardsThisTurn`），每 3 张触发一次，金币获得量 = 5 × 贪婪小丑数量
- **GluttonyJoker**：同上，每 4 张技能牌回复 3 × 暴食小丑数量点生命（使用 `HealCmd`）
- **MirrorJoker**：在 `ModifyCardPlayCount` 中检测 `LastCardTypeThisTurn == card.Type`，额外触发次数 = 镜像小丑数量
- **MiserJoker**：在 `ModifyDamageAdditive` 中计算手牌中 0 费牌数量（不含 X 费），加法加成 = 0 费牌数 × 守财小丑数量
- **CollectorJoker**：在 `AfterPlayerTurnStart` 中统计牌组 Rare/Ancient 牌，能量 = `floor(RareAncientCount / 5) × 收藏家数量`
- **GamblerJoker**：在 `AfterCardPlayed` 中检测连击 ≥ 5，随机目标 8~20 伤害（`GamblerJokerMinDamage` + `Random.Range(0, GamblerJokerMaxDamage - GamblerJokerMinDamage)`）
- **PolychromeJoker**：在 `ModifyCardPlayCount` 中对有修饰器的卡牌额外 +1 触发（与多彩修饰器本身的 +1 叠加，共 +2）
- **NegativeJoker**：通过 `AcquireJoker` 的特殊处理：获得时自动解锁第 4、5、6 槽（`YUWANCARD_UnlockedJokerSlots = Math.Max(current, 5)`）
- **LegendJoker**：`GetLegendBonus()` 返回 `CardsPlayedThisTurn × 0.2 × LegendJokerCount`，加入 `ComboMultiplier` 计算
- **HolographicJoker**：`AfterPlayerTurnStart` 中从 `YUWANCARD_PreviousTurnFirstCardJson` 反序列化上回合第一张牌，复制加入手牌。上回合的第一张牌在 `AfterCardPlayed` 中首次打出时通过 JSON 序列化存储到 `YUWANCARD_CurrentTurnFirstCardJson`
- **BankerJoker**：利息计算中 `+3 × BankerJokerCount`
- **InvestorJoker**：在 `AfterItemPurchased` 中返还 `floor(goldSpent × 0.2 × Count)`

---

## 9. 配套遗物

9 个普通遗物继承 `BalatroRelicModel`（位于 `Relics/Balatro/BalatroRelicModel.cs`），注册在 `SharedRelicPool`。仅在 Balatro 模式激活时出现（`IsAllowed` 检查 `BalatroModifier.IsActive`）。

| 名称 | 稀有度 | 效果 |
|------|--------|------|
| **Dice** (骰子) | Common | 每回合开始时，随机将连击设为 1~3（不降低已有连击） |
| **Chip** (筹码) | Common | 战斗开始时，连击 +3 |
| **WildCard** (万能牌) | Uncommon | 打出状态牌计入连击（+0.5），诅咒牌额外 +2 |
| **SteelJoker** (钢制小丑) | Uncommon | 回合结束时保留 20% 连击 |
| **GrowingJoker** (成长小丑) | Uncommon | 每次给卡牌添加修饰器，永久获得 3 最大生命值 |
| **BlankVoucher** (空白兑换券) | Rare | 获得时从 3 个随机 Joker 中选择 1 个获得 |
| **Blueprint** (蓝图) | Rare | 复制最右侧 Joker 槽位的效果 |
| **LuckyCard** (幸运卡) | Ancient | 每打出第 7 张牌，该牌额外触发 2 次 |
| **ModifierToken** (修饰器代币) | None | 获得时给予 1 个修饰器代币，随后自毁（不在任何遗物池，仅通过精英/Boss 奖励掉落） |

### 配套遗物实现详解

- **Dice**：`AfterPlayerTurnStart` 中 `ComboCounter = Math.Max(ComboCounter, Random.Range(1, 4))`
- **Chip**：`BeforeCombatStart` 中 `ComboCounter += 3`
- **WildCard**：在连击计算时允许状态牌和诅咒牌计入
- **SteelJoker**：在 `AfterTurnEnd` 中计算保留比例时取 Max(0.1, 0.2)
- **GrowingJoker**：`BalatroCardEditionHelper.TryApplyEdition` 中检测并触发 `GainMaxHp(3)`
- **BlankVoucher**：`AfterObtained` 中从可用 Joker 池随机 3 个，弹出选择界面
- **Blueprint**：在 `GetAllJokerSlotIds` 等效逻辑中，将最右侧非空 Joker ID 加入生效列表
- **LuckyCard**：`ModifyCardPlayCount` 中检测 `CardsPlayedThisTurn % 7 == 0`，额外 +2
- **ModifierToken**：`AfterObtained` 中 `modifier.AddModifierTokens(1)` 后 `RelicCmd.Remove` 自毁

---

## 10. Balatro 能力

| 能力 | 类型 | StackType | 来源 | 效果 |
|------|------|-----------|------|------|
| **CompoundInterestPower** | Buff | Counter | 复利卡牌 | Amount 层数 × 5 的利息上限加成 |
| **InflationPower** | Buff | Counter | 通货膨胀卡牌 | 获得金币 +Amount%，非 X 费卡牌费用 +1 |
| **InertiaPower** | Buff | Counter | 连击 ≥ 20 自动获得 | 下回合保留 10% 连击，回合开始时自毁 |

### InflationPower 特殊实现

使用 `GoldModificationGuard` 来防止无限递归（金币加成触发自身）。`ShouldGainGold` 和 `AfterGoldGained` 代理给 Guard 处理，确保每次金币获得事件只触发一次额外金币。

### InertiaPower 生命周期

1. 回合结束时，若 `ComboCounter ≥ 20`，获得 1 层 InertiaPower
2. InertiaPower 内部存储保留的缩放连击值
3. 下回合开始时，`AfterPlayerTurnStart` 将保留的连击恢复到 `ComboCounter`
4. InertiaPower 自毁（`PowerCmd.Remove`）

---

## 11. UI 与交互

### 11.1 UI 主题系统

`BalatroUiTheme`（`UI/BalatroUiTheme.cs`）是 Balatro 模式所有 UI 的**统一视觉主题**，提供：

| 元素 | 颜色 | 用途 |
|------|------|------|
| Surface | `#1A1817` (暗棕) | 面板背景 |
| SurfaceAlt | `#242220` (稍亮棕) | 卡片背景 |
| SurfaceHover | `#2B2724` (悬停棕) | 悬停态 |
| SurfacePressed | `#141311` (深压棕) | 按下态 |
| Border | `#BAAA8F` (淡金) | 默认边框 |
| BorderStrong | `#EDDBB5` (亮金) | 强调边框 |
| Title | `#F7EDDB` (暖白) | 标题文字 |
| Body | `#DED9CF` (灰白) | 正文 |
| Muted | `#A8A399` (灰) | 次要文字 |
| Accent | `#E8D1A0` (金) | 强调色 |
| Price | `#F2D670` (亮金) | 价格文字 |

**工厂方法**：
- `CreatePanelStyle()` — 面板 StyleBox（2px 金色边框，12px 圆角，阴影）
- `CreateCardStyle(bg?, border?)` — 卡片 StyleBox（1px 边框，10px 圆角）
- `CreateTextLabel(text, fontSize, color, ...)` — 统一文本标签
- `CreateGlyphIcon(glyph, accentColor, size)` — 文本图标（带边框面板）
- `CreateTextureIcon(texture, size)` — 纹理图标（带边框面板）

**按钮样式方法**：
- `ApplyCardButtonStyle(button)` — 卡片按钮（完整状态样式）
- `ApplyActionButtonStyle(button, primary)` — 操作按钮（主要/次要）
- `ApplySlotButtonStyle(button, selected, unlocked)` — 槽位按钮（选中/未解锁态）

**修饰器辅助**：
- `GetEditionGlyph(edition)` — 获取修饰器缩写（FL/HO/PC/NG）
- `GetEditionAccent(edition)` — 获取修饰器强调色

### 11.2 角色选择 Tickbox

- 文件：`Patches/BalatroCharacterSelectPatch.cs`
- 注入点：`NCharacterSelectScreen` 的 `_Ready`、`SelectCharacter`、`OnSubmenuOpened`、`InitializeSingleplayer`、`InitializeMultiplayerAsHost/Client`
- 创建 `NRunModifierTickbox` 放置在 AscensionPanel 右侧
- 客户端只读同步（通过 SavedProperty 自动同步）

### 11.3 HUD 面板

- 文件：`UI/NBalatroHudPanel.cs`
- 显示内容：
  - **战斗中**：`COMBO 12.0  MULT x2.2`（连击数和乘数）
  - **始终**：Joker 槽位栏（`NJokerSlotBar`，嵌入在面板中）
- 交互：
  - 可拖动（支持鼠标和触摸）
  - 自动限制在视口范围内
  - 通过顶部栏 Balatro 图标按钮切换显示/隐藏
- 视觉：深色半透明背景 + 金色边框圆角面板（`BalatroUiTheme.CreatePanelStyle()`）

### 11.4 Joker 槽位栏

- 文件：`UI/NJokerSlotBar.cs`
- 嵌入在 `NBalatroHudPanel` 中，显示 6 个 Joker 槽位按钮
- 每个槽位按钮显示：
  - 未解锁 → 灰色虚线框 + 🔒 符号，不可点击
  - 空槽位 → 按钮显示槽位编号 + "空"文字
  - 已装备 → 按钮显示编号 + Joker 名称缩写（≤8 字符）
- 右侧「背包」按钮显示背包中 Joker 数量
- 点击任意槽位或背包按钮 → 打开 `NJokerBagPopup`
- 每帧 `_Process` 中更新槽位状态（`BalatroModifier` 实例变化时自动同步）

### 11.5 Joker 背包弹窗

- 文件：`UI/NJokerBagPopup.cs`
- 作为模态弹窗（`NModalContainer`）打开
- 上方：6 个槽位按钮（选中高亮，未解锁灰色），点击切换目标槽位
- 中间：2 列网格显示背包中所有 Joker（卡片式布局，含图标、名称、描述）
- 下方：
  - 当前选中槽位信息
  - 「卸下」按钮（将当前槽位 Joker 移回背包）
  - 「关闭」按钮
- 点击背包中的 Joker → 装备到当前选中槽位
- 实现 `IScreenContext`，支持手柄导航（`DefaultFocusedControl`）

### 11.6 商店加工站扩展

- 文件：`UI/NBalatroMerchantExtension.cs` + `Patches/BalatroMerchantPatch.cs`
- `BalatroMerchantPatch` 注入 `NMerchantInventory`：
  - `_Ready` → 创建 `NBalatroMerchantExtension` 并添加到 `SlotsContainer`
  - `Open` → 调用 `RefreshForOpen()` 刷新商品和状态
  - `Close` → 调用 `OnInventoryClosed()` 重置界面
- `NBalatroMerchantExtension` 提供两个标签页：
  - **商店**（默认）：隐藏加工站面板，显示原始商店内容
  - **加工站**：隐藏原始商店内容，显示修饰器购买界面
- 加工站界面布局：
  - 标题和描述（本地化）
  - 当前代币数量
  - 2 个修饰器商品卡片（带图标、名称、描述、价格）
  - 「刷新」按钮（25 金币，禁用时灰色显示）
- 点击商品 → 扣除金币/代币 → 弹出卡牌选择 → 应用修饰器
- 购买流程中使用 `RunWithMerchantHiddenAsync` 隐藏商店以避免 UI 冲突

### 11.7 顶部栏按钮

- 文件：`Patches/BalatroUiPatches.cs`
- 在 `NTopBar` 上注入一个 Balatro 图标按钮（`images/modifiers/balatro.png`）
- 仅在 Balatro 模式激活时可见
- 点击切换 HUD 面板的显示/隐藏

### 11.8 顶部栏修饰器图标

- 文件：`Patches/BalatroTopBarModifierFilterPatch.cs`
- `NTopBarModifier` 显示 Balatro 修饰器图标时使用 `BalatroModifier.Icon` 替代默认图标

---

## 12. 文件结构

```
YuWanCardCode/
├── Balatro/                                    # 核心定义命名空间
│   ├── BalatroCardEdition.cs                   # 卡牌修饰器枚举 (None/Foil/Holographic/Polychrome/Negative)
│   ├── BalatroCardEditionHelper.cs             # 修饰器应用辅助类（含 GrowingJoker 联动）
│   ├── BalatroCardKeywords.cs                  # 自定义 CardKeyword（Foil/Holographic/Polychrome/Negative）
│   └── BalatroCardPool.cs                      # Balatro 卡牌池（共享无色，淡金色主题）
│
├── Modifiers/
│   └── BalatroModifier.cs                      # 修改器主体（~1200 行，所有核心逻辑）
│
├── Cards/Balatro/                              # 13 张 Balatro 卡牌
│   ├── Investment.cs                           #   1. 投资（经济）
│   ├── CompoundInterest.cs                     #   2. 复利（经济/能力）
│   ├── Dividend.cs                             #   3. 分红（经济/连击奖励）
│   ├── Bankruptcy.cs                           #   4. 破产（经济/攻击）
│   ├── Inflation.cs                            #   5. 通货膨胀（经济/能力）
│   ├── Magician.cs                             #   6. 魔术师（塔罗/转换）
│   ├── Priestess.cs                            #   7. 女祭司（塔罗/复制）
│   ├── Emperor.cs                              #   8. 皇帝（塔罗/减费）
│   ├── Death.cs                                #   9. 死神（塔罗/删除）
│   ├── Mercury.cs                              #  10. 水星（星球/攻击强化）
│   ├── Venus.cs                                #  11. 金星（星球/格挡强化）
│   ├── Obsidian.cs                             #  12. 黑曜石（光谱/多彩）
│   └── VoidCard.cs                             #  13. 虚空（光谱/负片）
│
├── Relics/Jokers/                              # 12 个 Joker 遗物 + 基类
│   ├── BalatroJokerRelicModel.cs               # Joker 遗物抽象基类
│   ├── GreedJoker.cs                           #   1. 贪婪小丑
│   ├── GluttonyJoker.cs                        #   2. 暴食小丑
│   ├── MirrorJoker.cs                          #   3. 镜像小丑
│   ├── MiserJoker.cs                           #   4. 守财小丑
│   ├── CollectorJoker.cs                       #   5. 收藏小丑
│   ├── GamblerJoker.cs                         #   6. 赌徒小丑
│   ├── PolychromeJoker.cs                      #   7. 多彩小丑
│   ├── NegativeJoker.cs                        #   8. 负片小丑
│   ├── LegendJoker.cs                          #   9. 传奇小丑
│   ├── HolographicJoker.cs                     #  10. 全息小丑
│   ├── BankerJoker.cs                          #  11. 银行家小丑
│   └── InvestorJoker.cs                        #  12. 投资家小丑
│
├── Relics/Balatro/                             # 9 个配套遗物 + 基类
│   ├── BalatroRelicModel.cs                    # Balatro 配套遗物抽象基类（IsAllowed 检查 BalatroModifier.IsActive）
│   ├── Dice.cs                                 # 骰子
│   ├── Chip.cs                                 # 筹码
│   ├── WildCard.cs                             # 万能牌
│   ├── SteelJoker.cs                           # 钢制小丑
│   ├── GrowingJoker.cs                         # 成长小丑
│   ├── BlankVoucher.cs                         # 空白兑换券
│   ├── Blueprint.cs                            # 蓝图
│   ├── LuckyCard.cs                            # 幸运卡
│   └── ModifierToken.cs                        # 修饰器代币（稀有度 None，自动自毁）
│
├── Powers/Balatro/                             # 3 个 Balatro 能力
│   ├── CompoundInterestPower.cs                # 复利能力（利息上限加成）
│   ├── InflationPower.cs                       # 通货膨胀能力（金币加成 + 费用增加）
│   └── InertiaPower.cs                         # 惯性能力（连击跨回合保留，自毁）
│
├── Patches/                                    # 6 个 Balatro 补丁文件
│   ├── BalatroCharacterSelectPatch.cs          # 角色选择界面 Tickbox
│   ├── BalatroUiPatches.cs                     # TopBar 按钮 + HUD 面板注入
│   ├── BalatroTopBarModifierFilterPatch.cs     # TopBar 修饰器图标替换
│   ├── BalatroMerchantPatch.cs                 # 商店加工站扩展注入
│   ├── BalatroCardEditionPersistencePatch.cs   # 修饰器序列化/反序列化/克隆持久化
│   └── BalatroCardEditionVisualPatch.cs        # 修饰器着色器视觉特效（NCard 叠加层）
│
└── UI/                                         # 5 个 Balatro UI 组件
    ├── BalatroUiTheme.cs                       # 统一视觉主题（颜色、样式、工厂方法）
    ├── NBalatroHudPanel.cs                     # 可拖动 HUD 面板（连击 + Joker 槽位栏）
    ├── NJokerSlotBar.cs                        # Joker 槽位栏组件（嵌入 HUD 面板）
    ├── NJokerBagPopup.cs                       # Joker 背包管理弹窗（装备/卸下/浏览）
    └── NBalatroMerchantExtension.cs            # 商店加工站标签页（购买修饰器）
```

### 资源文件

```
YuWanCard/
├── shaders/ui/
│   └── balatro_card_edition_border.gdshader   # 修饰器边框着色器（4 种模式：Foil/Holographic/Polychrome/Negative）
│
├── images/modifiers/
│   └── balatro.png                             # 修改器图标 + TopBar 按钮图标
│
└── localization/
    ├── eng/
    │   ├── card_keywords.json                  # 修饰器关键词（Foil/Holographic/Polychrome/Negative）
    │   ├── cards.json                          # 13 张卡牌本地化
    │   ├── gameplay_ui.json                    # UI 文本（加工站/背包/槽位）
    │   ├── modifiers.json                      # Balatro 修改器名称和描述
    │   ├── relics.json                         # 21 个遗物本地化（12 Joker + 9 配套）
    │   └── powers.json                         # 3 个能力本地化
    └── zhs/
        ├── card_keywords.json
        ├── cards.json
        ├── gameplay_ui.json
        ├── modifiers.json
        ├── relics.json
        └── powers.json
```

### 本地化 Key 命名规范

| 功能 | 前缀 | 示例 |
|------|------|------|
| 修饰器类型 | `YUWANCARD-BALATRO_EDITION.{TYPE}.` | `YUWANCARD-BALATRO_EDITION.FOIL.title` |
| 加工站 UI | `YUWANCARD-BALATRO_MOD_STATION.` | `YUWANCARD-BALATRO_MOD_STATION.title` |
| Joker 槽位栏 | `YUWANCARD-BALATRO_JOKER_BAR.` | `YUWANCARD-BALATRO_JOKER_BAR.bag_button` |
| Joker 背包 | `YUWANCARD-BALATRO_JOKER_BAG.` | `YUWANCARD-BALATRO_JOKER_BAG.title` |
| 修改器 | `YUWANCARD-BALATRO.` | `YUWANCARD-BALATRO.Name` |

---

## 13. 设计文档对照

本实现基于 `docs/superpowers/specs/2026-06-03-balatro-modifier-design.md` 设计文档。以下记录实现与设计的主要差异：

### 已完整实现

- 6 大核心子系统全部实现（Joker 槽位、连击乘数、卡牌修饰器、加工站、利息经济、消耗品卡牌）
- 12 个 Joker 遗物全部实现（效果与设计一致）
- 9 个配套遗物全部实现（含 ModifierToken）
- 13 张 Balatro 卡牌全部实现
- 3 个 Balatro 能力全部实现
- 角色选择 Tickbox
- 可拖动 HUD 面板（NBalatroHudPanel）
- Joker 槽位栏 UI（NJokerSlotBar）
- Joker 背包管理界面（NJokerBagPopup）
- 商店加工站标签页（NBalatroMerchantExtension）
- 卡牌修饰器着色器视觉特效（BalatroCardEditionVisualPatch）
- 修饰器序列化持久化（BalatroCardEditionPersistencePatch）
- 统一 UI 主题系统（BalatroUiTheme）
- 顶部栏 Balatro 按钮和修饰器图标

### 未实现（未来扩展）

- 连击增加/消耗粒子动画
- Joker 槽位独立可视化（当前使用文字 + 按钮，非 Spine/动画）
- 加工站修饰器图标（当前使用默认 pig_carrot.png 占位）
- 更多修饰器获取途径（如随机事件、宝箱掉落）

---

## 附录 A：核心代码路径速查

| 需求 | 入口 |
|------|------|
| 检查 Balatro 是否激活 | `BalatroModifier.IsActive(runState)` |
| 获取修改器实例 | `BalatroModifier.GetInstance(runState)` |
| 给卡牌加修饰器 | `BalatroCardEditionHelper.TryApplyEdition(card, edition)` |
| 检查卡牌能否加修饰器 | `card.CanApplyBalatroEdition(edition)` |
| 获取/设置卡牌修饰器 | `BalatroCardEditionHelper.GetEdition(card)` / `card.BalatroEdition` |
| 获取连击显示文本 | `modifier.GetComboDisplayText()` |
| 获取 Joker 显示文本 | `modifier.GetJokerDisplayText()` |
| 获取装备的 Joker 列表 | `modifier.GetEquippedJokerIds()` |
| 获取全部槽位状态 | `modifier.GetAllJokerSlotIds()` |
| 检查槽位是否解锁 | `modifier.IsJokerSlotUnlocked(slotIndex)` |
| 获取 Joker 槽位容量 | `modifier.GetCurrentJokerCapacity()` |
| Joker 背包操作 | `modifier.TryEquipBagJoker()` / `modifier.TryUnequipJoker()` |
| 获取 Joker 元数据 | `modifier.GetJokerTitle()` / `GetJokerDescription()` / `GetJokerIcon()` |
| 加工站商品操作 | `modifier.GetModStationOffers()` / `EnsureModStationOffers()` / `PurchaseModStationOffer()` |
| 修饰器代币 | `modifier.ModifierTokenCount` / `modifier.AddModifierTokens()` |
| 获取修饰器价格 | `modifier.GetEditionShopCost(edition)` |
| 获取 Balatro 卡池 | `ModelDb.CardPool<BalatroCardPool>()` |
| 检查卡牌是否有修饰器 | `BalatroCardEditionHelper.HasEdition(card)` |
| 序列化/反序列化修饰器 | `BalatroCardEditionHelper.WriteGenericEditionToSerializable()` / `RestoreGenericEditionFromSerializable()` |
| 复制修饰器到克隆 | `BalatroCardEditionHelper.CopyEditionStateToClone(source, clone)` |
| 获取修饰器缩写/颜色 | `BalatroUiTheme.GetEditionGlyph(edition)` / `GetEditionAccent(edition)` |

## 附录 B：SavedProperty 完整列表

Balatro 修改器通过以下 `[SavedProperty]` 属性持久化所有状态（多人游戏中自动同步）：

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `YUWANCARD_UnlockedJokerSlots` | int | 3 | 已解锁槽位数（3~6） |
| `YUWANCARD_RetainedComboScaled` | int | 0 | 跨回合保留连击（缩放整数） |
| `YUWANCARD_LastInterestFloor` | int | 0 | 上次触发利息的楼层号 |
| `YUWANCARD_JokerBag` | string | `""` | 背包中 Joker ID（`\|` 分隔） |
| `YUWANCARD_JokerSlot1Id` ~ `JokerSlot6Id` | string | `""` | 6 个槽位装备的 Joker ID |
| `YUWANCARD_CurrentTurnFirstCardJson` | string | `""` | 本回合第一张牌（JSON 序列化） |
| `YUWANCARD_PreviousTurnFirstCardJson` | string | `""` | 上回合第一张牌（JSON 序列化） |
| `YUWANCARD_ModifierTokens` | int | 0 | 修饰器代币数量 |
| `YUWANCARD_ModStationOffer1` | int | 0 | 加工站商品 1（枚举值） |
| `YUWANCARD_ModStationOffer2` | int | 0 | 加工站商品 2（枚举值） |
| `YUWANCARD_ModStationFloor` | int | 0 | 上次刷新加工站商品的楼层号 |

## 附录 C：卡牌修饰器 SavedProperty

每张卡牌（`CardModel`）通过以下 generic 数据持久化修饰器状态（由 `BalatroCardEditionPersistencePatch` 代理）：

| Key | 类型 | 说明 |
|-----|------|------|
| `YUWANCARD_Edition` | int | 修饰器枚举值（0=None, 1=Foil, 2=Holographic, 3=Polychrome, 4=Negative） |
| `YUWANCARD_FoilApplied` | bool | 铝箔数值加成是否已应用（防止重复应用） |
