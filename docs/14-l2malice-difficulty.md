# 莱特兰恶意难度

## 概述

莱特兰恶意（L2 Malice）是一个独立于原版 Ascension 的平行难度系统，从 Minecraft mod [莱特兰恶意](https://www.mcmod.cn/class/12008.html) 移植而来。核心特色是 **敌人获得随机的"词条"（Traits）** ——不仅仅是数值放大，而是获得特殊能力，如再生、荆棘、分裂、复活等。

不同于 Ascension 仅改变数值，Malice 通过词条系统让每场战斗的敌人组合更加多变，提升了重复游玩的可玩性。

## 与 Ascension 的关系

| | Ascension（进阶） | Malice（恶意） |
|---|---|---|
| **等级范围** | 0-10 | 0-10 |
| **解锁方式** | 胜利后解锁下一级 | 独立解锁（不超过 MaxAscension） |
| **核心机制** | 数值调整（HP/伤害/金币/掉落等） | 词条分配（敌人获得随机能力） |
| **同时生效** | 是 | 是 |
| **UI** | 红色火焰图标 | 紫色恶意图标 |

两者可以同时开启（例如 Ascension 5 + Malice 3），词条效果会叠加在 Ascension 已经增强的敌人之上。

## 难度等级

Malice 共 10 个等级，效果累积：

| 等级 | 名称 | 效果 |
|------|------|------|
| 1 | 恶意初现 | 所有敌人获得 1 个常见词条 |
| 2 | 词条扩散 | 敌人 HP +5% |
| 3 | 加深诅咒 | 精英敌人词条上限 +1 |
| 4 | 腐化之力 | 罕见词条解锁；普通敌人词条上限 +1 |
| 5 | 深渊凝视 | 敌人伤害 +10% |
| 6 | 灵魂灼烧 | 稀有词条解锁；精英敌人词条上限再 +1 |
| 7 | 绝望蔓延 | Boss 词条上限 +1 |
| 8 | 永恒诅咒 | 敌人 HP +15%（覆盖 Lv2 的 5%） |
| 9 | 地狱之门 | 传说词条解锁 |
| 10 | 终焉恶意 | Boss 词条上限再 +1 |

## 词条系统

词条是附加在敌人身上的临时 Buff（Power），在战斗开始时随机分配，敌人死亡时移除。词条分为 4 个稀有度，分别在特定 Malice 等级解锁。

### 常见词条（Common，Malice 1+）

| 词条 | 效果 |
|------|------|
| Tank（坦克） | +25% MaxHP |
| Speedy（迅捷） | 获得 敏捷（每层 +1） |
| Regen（再生） | 每回合回复 MaxHP 的 5% |
| Fiery（荆棘） | 被攻击时对攻击者造成 1 点伤害 |
| Weakness（虚弱） | 攻击命中时施加 1 层 虚弱 |
| Slowness（迟缓） | 攻击命中时施加 1 层 脆弱 |
| Gravity（重力） | 每 2 回合使所有玩家失去力量，上限随幕数增加 |

### 罕见词条（Uncommon，Malice 4+）

| 词条 | 效果 |
|------|------|
| Poison（中毒） | 攻击命中时施加 1 层 中毒 |
| Reflect（反射） | 受到伤害后反射 3 点伤害 |
| Protection（保护） | 获得 覆甲（每层 +2） |
| Wither（凋零） | 攻击命中时减少玩家 力量 |
| Blindness（致盲） | 攻击命中时向玩家抽牌堆塞入 晕眩 |
| Shulker（潜影） | 己方回合开始时获得 2 层 滑溜。上限4层 |

### 稀有词条（Rare，Malice 6+）

| 词条 | 效果 |
|------|------|
| Drain（嗜魔） | 攻击命中时移除玩家 1 个随机 Buff，获得 1 力量 |
| Growth（成长） | 每回合获得 +2 力量 |
| Counter Strike（反击） | 受到伤害后 30% 概率对攻击者造成 6 点伤害 |
| Corrosion（腐蚀） | 攻击命中时玩家失去 1 能量 |
| Adaptive（适应） | 来自相同来源的连续伤害降低 30% |
| Invisible（隐形） | 己方回合开始时若没有 缓冲 则获得 缓冲 |
| Dispell（破魔） | 使卡牌上的附魔失效。|

### 传说词条（Legendary，Malice 9+）

| 词条 | 效果 |
|------|------|
| Undying（不朽） | 首次死亡时复活并回复 50% HP（一次性） |
| Dementor（摄魂） | 攻击时补上被格挡掉的伤害 |
| Split（分裂） | 死亡时生成 2 个1/4副本（无词条） |
| Master（主宰） | 己方回合开始时召唤随机小怪 |
| Killer Aura（杀手光环） | 玩家攻击牌费用 +1 |
| Ragnarok（诸神黄昏） | 每回合使玩家的一个遗物失效 |

## 词条分配机制

词条在敌人进入战斗时通过 `MaliceTraitDistributor` 自动分配：

1. **预算计算**
   - 普通敌人：Malice 1+ 有 1 预算，Malice 4+ 有 2 预算
   - 精英敌人：Malice 1+ 有 1 预算，Malice 3+ 有 2 预算，Malice 6+ 有 3 预算
   - Boss：Malice 1+ 有 1 预算，Malice 7+ 有 2 预算，Malice 10+ 有 3 预算
   - 召唤物（Minion）：固定 1 预算

2. **稀有度门槛**
   - 根据当前 Malice 等级确定可用稀有度池
   - | 稀有度 | 解锁等级 | 数量 |
     |--------|----------|------|
     | 常见（Common） | Malice 1+ | 7 |
     | 罕见（Uncommon） | Malice 4+ | 6 |
     | 稀有（Rare） | Malice 6+ | 7 |
     | 传说（Legendary） | Malice 9+ | 6 |
   - 召唤物（Minion）只能获得常见和罕见词条

3. **词条权重**（按幕数，非恶意等级）
   - 普通敌人：
     | 幕 | 常见 | 罕见 | 稀有 | 传说 |
     |----|------|------|------|------|
     | 1  | 70   | 30   | 0    | 0    |
     | 2  | 50   | 35   | 15   | 0    |
     | 3  | 35   | 35   | 22   | 8    |
     | 4+ | 25   | 35   | 25   | 15   |
   - 召唤物：
     | 幕 | 常见 | 罕见 |
     |----|------|------|
     | 1  | 80   | 20   |
     | 2  | 60   | 40   |
     | 3  | 40   | 60   |
     | 4+ | 20   | 80   |

4. **分配方式**
   - 从可用池中随机不重复选取词条，最多选取预算数量个
   - 只要敌人有预算，就会实际获得词条；当前实现不再对单个敌人额外做“概率跳过”

5. **标记**
   - 分配成功后施加 `MaliceTraitMarkerPower`（不可见的 Counter Power），用于标识词条敌人（影响遗物效果和击杀统计）

## 多人模式

- 恶意难度在多人模式下采用和原版多人进阶相同的主机权威逻辑
- 只有主机可以在角色选择界面调整恶意难度
- 客机面板只显示主机当前选择的恶意等级，不能本地修改，也不会把本地偏好写回 Lobby
- 开局时使用主机确认后的 `Lobby.Modifiers` / pending modifiers 作为恶意来源，避免主客机各自读取本地偏好导致不同步

## 7 宗罪遗物

将 L2Hostility 的 7 宗罪诅咒移植为恶意遗物，只有在恶意难度下才会出现。恶意遗物不进入通用遗物池，而是从精英和 Boss 奖励中掉落：

- **精英奖励**：25% 概率随机掉落一件恶意遗物
- **Boss 奖励**：100% 概率随机掉落一件恶意遗物

| 遗物 | 稀有度 | 效果 |
|------|--------|------|
| 恶意·嫉妒（Envy） | Rare | 若本场战斗击杀过词条敌人，额外获得一次卡牌奖励（4 选 1） |
| 恶意·贪婪（Greed） | Rare | 若本场战斗击杀过词条敌人，每个击杀额外获得 20 金币 |
| 恶意·暴怒（Wrath） | Rare | 对词条敌人的伤害 +75% |
| 恶意·傲慢（Pride） | Rare | 你的伤害使敌人生命值减少时，获得 1 力量 |
| 恶意·懒惰（Sloth） | Rare | 击杀词条敌人获得 8 格挡；禁用敌人的 HP 缩放 |
| 恶意·暴食（Gluttony） | Rare | 击杀词条敌人回复 8 HP |
| 恶意·色欲（Lust） | Rare | 若本场战斗击杀过词条敌人，有 25% 概率额外获得一件遗物 |

## 角色进度

每个角色有独立的 Malice 进度：

- **MaxMalice**：已解锁的最高等级
- **PreferredMalice**：当前选择的等级
- 数据存储在 `%AppData%/SlayTheSpire2/malice_progress.json`
- 等级上限不超过该角色的 MaxAscension
- 胜利时若当前 Malice = MaxMalice 且 MaxMalice < 10，则 MaxMalice +1

Daily / Custom 模式不影响 Malice 进度。多人模式下恶意仍由主机统一选择，但不会把客机本地偏好带入对局。

## 代码架构

```
YuWanCardCode/
├── Modifiers/
│   └── MaliceModifier.cs              # 恶意难度修饰符
├── Powers/MaliceTraits/               # 26 个词条 Power
│   ├── MaliceTraitPowerBase.cs        # 词条基类（PowerType=Buff, StackType=Counter）
│   ├── TankTrait.cs, SpeedyTrait.cs, ...
│   └── RagnarokTrait.cs
├── Relics/Malice/                     # 7 宗罪遗物
│   ├── EnvyMalice.cs, GreedMalice.cs, ...
│   └── LustMalice.cs
├── Malice/
│   ├── MaliceManager.cs               # 进度管理
│   ├── MaliceHelper.cs                # 条件查询（HasMalice 等）
│   └── MaliceTraitDistributor.cs      # 词条随机分配器
└── Patches/
    ├── MaliceCharacterSelectPatch.cs   # 角色选择 UI
    ├── MaliceRunPatches.cs             # 开局修饰符同步、胜利进度推进
    └── MaliceTopBarPatch.cs            # 顶栏恶意提示
```

### 关键类说明

**MaliceModifier**：继承 `YuWanModifierModel`，存储 `YuWanCard_MaliceLevel` 和 `YuWanCard_MaliceTraitKills`，通过 `BeforeCombatStartLate`、`AfterCreatureAddedToCombat` 和 `AfterPowerAmountChanged` 触发词条分配与召唤物补发。

**MaliceHelper**：静态辅助类，提供 `HasMalice(level)`、`GetMaliceLevel()`、`IsTraitEnemy(creature)` 等查询方法，可在任何需要感知 Malice 状态的地方使用。

**MaliceTraitPowerBase**：所有词条的基类，继承 `YuWanPowerModel`，统一设置 `Type = Buff`、`StackType = Counter`。`Amount` 表示词条的 Rank（用于强度叠加）。

**MaliceTraitDistributor.AssignTraits()**：核心分配逻辑，根据 Malice 等级和敌人类型计算预算，从对应稀有度池随机选取词条。

### 创建新词条

在 `YuWanCardCode/Powers/MaliceTraits/` 下创建类，继承 `MaliceTraitPowerBase`：

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

// 攻击命中时施加 debuff 的词条（参考 Weakness/Slowness）
public sealed class ExampleTrait : MaliceTraitPowerBase
{
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner) return;
        foreach (var result in command.Results)
        {
            if (result.Receiver.IsPlayer && !result.Receiver.IsDead)
            {
                Flash();
                // 对玩家施加效果
                await PowerCmd.Apply<SomePower>(result.Receiver, Amount, Owner, null);
            }
        }
    }
}
```

常用的生命周期钩子：

| 钩子 | 用途 | 示例 |
|------|------|------|
| `AfterApplied` | 施加时一次性效果 | Tank（+MaxHP）、Speedy（+敏捷） |
| `AfterSideTurnStart` | 每回合持续效果 | Regen（回血）、Growth（+力量）、Gravity（减力量） |
| `AfterAttack` | 攻击命中后 | Weakness、Drain、Blindness |
| `AfterDamageReceived` | 受到伤害后 | Fiery（荆棘）、Reflect（反射）、CounterStrike（反击） |
| `AfterDamageGiven` | 造成伤害后 | Dementor（补上格挡伤害） |
| `ModifyDamageMultiplicative` | 伤害倍率修改 | Adaptive（适应减伤） |
| `ShouldDie` / `AfterPreventingDeath` | 死亡干预 | Undying（复活） |
| `AfterDeath` | 死亡后 | Split（分裂） |

添加新词条后需要：在 `MaliceTraitDistributor.TraitPool` 中注册、添加本地化（`powers.json`）、添加图标（`images/powers/{power_id}.png`）。

## 实现特点

- **不修改敌人原始数据**：词条以 Power 形式附加，不改变敌人的 `MonsterModel` 定义
- **运行时动态分配**：每场战斗独立分配词条，同一敌人不同战斗可有不同词条组合
- **可关闭**：懒惰遗物可禁用敌人的 HP 缩放，降低词条敌人的坦度
- **与原版 Ascension 并行**：两者互不影响，可自由组合

## 版权

词条系统设计参考 [莱特兰恶意 (L2Hostility)](https://www.mcmod.cn/class/12008.html)，LGPL 2.1 许可。词条图标移植自 L2Hostility，详见 `L2HOSTILITY_LICENSE.txt`。
