# Mod 联动

## 概述

YuWanCard 通过**运行时兼容层（Runtime Compat）**与其他模组联动——无需编译时依赖，当检测到目标模组已加载时，通过反射 + Harmony Patch 自动注入自定义内容。

设计原则：
- **零编译时依赖**：不引用目标模组的 DLL，完全在运行时集成
- **可选联动**：目标模组未安装时，联动代码静默跳过，不影响主模组运行
- **向后兼容**：联动层独立于主模组逻辑，目标模组更新不会导致编译错误
- **通用基础设施**：`ModCompat` 工具类位于 `Core/Interop/`，可复用于任意模组

## 核心基础设施

### ModCompat / ModCompatContext

`Core/Interop/ModCompat.cs` 提供跨模组运行时兼容的通用工具。

```csharp
// 检查目标模组是否已加载
bool loaded = ModCompat.IsLoaded("TargetModId");

// 获取目标模组的 Assembly
if (ModCompat.TryGetAssembly("TargetModId", out Assembly? assembly))
{
    // 通过反射操作目标模组
}

// 创建兼容上下文（推荐方式，模组未加载时返回 null）
ModCompatContext? ctx = ModCompat.TryCreate("TargetModId", "MyIntegration");
if (ctx == null) return; // 目标模组未安装，静默跳过

// 解析目标模组中的类型（不需要完整程序集限定名）
Type? type = ctx.ResolveType("TargetMod.SomeClass");

// 批量注册 Harmony Patch
ctx.PatchMethods(
    harmony, targetType, typeof(MyPatches),
    prefixName: nameof(MyPrefix),
    postfixName: nameof(MyPostfix),
    "MethodA", "MethodB", "MethodC");
```

`ModCompatContext` 利用 `ModManager.GetLoadedMods()` 发现已加载模组。未找到时，`TryCreate` 返回 `null`，整个联动代码路径短路。

### 关键 API

| 方法 | 用途 |
|------|------|
| `ModCompat.IsLoaded(modId)` | 检查模组是否加载 |
| `ModCompat.TryGetAssembly(modId, out asm)` | 获取模组 Assembly |
| `ModCompat.TryCreate(modId, logPrefix)` | 创建 `ModCompatContext`（推荐入口） |
| `ctx.ResolveType(typeName)` | 从目标程序集解析类型 |
| `ctx.PatchMethod(harmony, targetType, methodName, patchOwner, prefix, postfix)` | 单方法 Patch |
| `ctx.PatchMethods(harmony, targetType, patchOwner, prefix, postfix, methodNames...)` | 批量 Patch |

## 联动架构模式

一个完整的联动模块通常包含以下层次：

```
YuWanCardCode/Integrations/{TargetMod}/
├── {Target}RuntimeCompat.cs       # 入口：检测加载、注册 Harmony Patch
├── {Target}Registry.cs            # 注册表：管理自定义内容列表（可选）
├── {Target}SharedState.cs         # 共享状态 / 辅助方法（可选）
├── {Type}PoolKey.cs               # 池 Key 常量（可选）
├── SomeEnum.cs                    # 自定义枚举（可选）
├── RelicPools/
│   └── {Target}Pool.cs            # 自定义遗物池（可选）
├── Relics/
│   ├── {Target}Base.cs            # 自定义遗物基类
│   ├── {Target}SharedBase.cs      # 共享遗物基类（全角色可用）
│   └── ConcreteRelic.cs           # 具体遗物实现
└── Powers/
    └── SomePower.cs               # 联动需要的 Power（可选）
```

### 入口类（RuntimeCompat）

入口类的职责：
1. **防重复安装**：用 `bool _installed` 标记确保只初始化一次
2. **检测目标模组**：通过 `ModCompat.TryCreate` 检查是否已加载
3. **注册 Harmony Patch**：向目标模组的类型注入 Postfix/Prefix
4. **处理边界情况**：目标类型不存在时 `Warn` 而非崩溃

```csharp
public static class MyTargetRuntimeCompat
{
    private const string TargetModId = "TargetModId";
    private static bool _installed;

    public static void TryInstall(Harmony harmony)
    {
        if (_installed) return;

        ModCompatContext? ctx = ModCompat.TryCreate(TargetModId, "MyTargetCompat");
        if (ctx == null) return;

        _installed = true;
        // 注册各种 Patch...
    }
}
```

### 注册表（Registry）

当联动涉及多个内容项时，用注册表集中管理列表和查询逻辑：

- **按分类组织内容列表**（稀有度、类型等）
- **提供查询方法**（`IsXxx`, `TryGetXxx`, `GetXxxByCategory`）
- **互斥规则**（`GetMutuallyExclusiveIds`）
- **可用性检查**（`IsAvailableForPlayer`, `IsAllowedInAct`）

### 遗物基类

根据内容定位，通常需要多个基类：

| 基类类型 | 用途 | 注册池 | `IsAvailableForPlayer` |
|---------|------|--------|----------------------|
| 专属基类 | 仅本角色可用 | 自定义池 | 检查角色 ID |
| 共享基类 | 全角色可用 | `SharedRelicPool` | `return true` |

基类负责统一设置：
- `Rarity` — 通常设为 `RelicRarity.None`（由目标模组管理稀有度）
- `IconBasePath` — 统一图标路径
- `CustomRarityLabelKey` — 自定义稀有度标签（在 UI 中显示）

### Harmony 注入点

常见的 Patch 模式：

1. **内容列表注入**：Postfix 目标模组的"获取全部内容"方法，`Concat` 自定义内容列表
2. **可用性检查**：Postfix 目标模组的 `IsAvailable` 方法，针对自定义内容返回正确结果
3. **池标识**：Postfix 目标模组的池 Key 查询，返回自定义 Key
4. **图鉴兼容**：Patch 原版的 `UnlockState`、`IsRelicSeen`，确保自定义内容在图鉴中可见
5. **互斥注入**：Postfix 目标模组的互斥查询，合并自定义互斥规则

## 创建联动：步骤指南

### 1. 创建目录结构

在 `YuWanCardCode/Integrations/` 下创建以目标模组命名的子目录，按需建立 `Relics/`、`Powers/`、`RelicPools/` 等子目录。

### 2. 定义枚举和常量

若目标模组使用枚举分类（如稀有度），创建对应的枚举供基类使用：

```csharp
namespace YuWanCard.MyTarget;

public enum MyTargetRarity { Common, Rare, Epic }
```

### 3. 编写遗物基类

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(MyTargetPool))]
public abstract class MyTargetRelicBase : YuWanRelicModel
{
    public sealed override RelicRarity Rarity => RelicRarity.None;

    protected override string IconBasePath
        => $"res://YuWanCard/images/integrations/my_target/relics/{RelicId}";

    public sealed override string? CustomRarityLabelKey
        => "YUWANCARD-MY_TARGET_RARITY.label";

    public abstract MyTargetRarity MyTargetRarity { get; }

    public virtual bool IsAvailableForPlayer(Player player)
        => player.Character.Id == ModelDb.GetId<Characters.Pig>();

    protected MyTargetRelicBase() : base(true) { }
}
```

### 4. 实现具体遗物

标准模式与普通遗物完全相同：

```csharp
public sealed class MyRelic : MyTargetRelicBase
{
    public override MyTargetRarity MyTargetRarity => MyTargetRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars
        => [new PowerVar<StrengthPower>(2m)];

    public override async Task BeforeCombatStart()
    {
        if (Owner == null) return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature,
            DynamicVars.Strength.BaseValue, Owner.Creature, null);
    }
}
```

### 5. 编写注册表

```csharp
public static class MyTargetRegistry
{
    private static readonly IReadOnlyList<Type> CommonItems = [typeof(ItemA), typeof(ItemB)];
    private static readonly IReadOnlyList<Type> RareItems = [typeof(ItemC)];

    public static IReadOnlyList<Type> GetAll() => CommonItems.Concat(RareItems).ToArray();

    public static IReadOnlyList<Type> GetByRarity(MyTargetRarity r) => r switch
    {
        MyTargetRarity.Common => CommonItems,
        MyTargetRarity.Rare => RareItems,
        _ => Array.Empty<Type>()
    };

    public static bool IsMyRelic(RelicModel? relic)
    {
        if (relic == null) return false;
        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return GetAll().Any(t => ModelDb.GetId(t) == id);
    }

    // TryGetRarity, IsAvailableForPlayer, IsAllowedInAct 等...
}
```

### 6. 编写入口兼容类

```csharp
public static class MyTargetRuntimeCompat
{
    private const string ModId = "TargetModId";
    private static bool _installed;

    public static void TryInstall(Harmony harmony)
    {
        if (_installed) return;
        ModCompatContext? ctx = ModCompat.TryCreate(ModId, "MyTargetCompat");
        if (ctx == null) return;
        _installed = true;

        // Patch 目标模组的目录方法
        Type? catalogType = ctx.ResolveType("TargetMod.Catalog");
        ctx.PatchMethods(harmony, catalogType, typeof(MyTargetRuntimeCompat),
            null, nameof(GetAllItemsPostfix), "GetAllItems");
        ctx.PatchMethods(harmony, catalogType, typeof(MyTargetRuntimeCompat),
            null, nameof(IsAvailablePostfix), "IsAvailableForPlayer");
    }

    public static void GetAllItemsPostfix(ref IReadOnlyList<Type> __result)
    {
        __result = __result.Concat(MyTargetRegistry.GetAll()).Distinct().ToArray();
    }

    public static void IsAvailablePostfix(RelicModel relic, Player player, ref bool __result)
    {
        if (MyTargetRegistry.IsMyRelic(relic))
            __result = MyTargetRegistry.IsAvailableForPlayer(relic, player);
    }
}
```

### 7. 在初始化中注册

在 `MainFile` 初始化流程中调用：

```csharp
public override void Initialize()
{
    // ... 其他初始化 ...

    // 在 ModLifecycle 的合适阶段注册联动
    ModLifecycle.OnContentRegistering(() =>
    {
        MyTargetRuntimeCompat.TryInstall(Harmony);
    });
}
```

### 8. 添加本地化

- 遗物名称/描述：`relics.json` 中添加对应 key
- 稀有度标签：`relics.json` 中添加 `CUSTOM_RARITY.label`
- 图鉴分类：`relic_collection.json` 中添加分类 key
- Power 名称/描述（如有）：`powers.json`

### 9. 添加图标

遗物图标放在 `YuWanCard/images/integrations/{target_mod}/relics/{relic_id}.png`，与基类中设置的 `IconBasePath` 一致。

### 10. 处理边界情况

- **目标类型不存在**：`ctx.ResolveType` 返回 `null` 时记录 Warn，不要抛异常
- **方法签名变化**：`PatchMethod` 找不到方法时自动记录 Warn 并返回 `false`
- **重复加载**：用 `_installed` 标记防重复安装
- **反射方法不存在**：Patch 非关键方法（如图鉴显示）时，方法不存在应静默跳过

## 案例：海克斯模组联动

海克斯符文（HextechRunes）是首个基于此架构实现的联动，可作为参考实现。

### 目录结构

```
YuWanCardCode/Integrations/Hextech/
├── HextechRuntimeCompat.cs            # 入口：三层 Harmony Patch（目录/识别/图鉴）
├── HextechPigRuneRegistry.cs          # 注册表：符文列表、稀有度、互斥、幕数限制
├── HextechForgeRegistry.cs            # 注册表：锻造列表
├── HextechPigRuneSharedState.cs       # 共享状态：七宗罪指环联动增强
├── HextechRuneRarity.cs               # 枚举：Silver / Gold / Prismatic
├── HextechForgeRarity.cs              # 枚举：Silver / Gold / Prismatic
├── HextechRunePoolKey.cs              # 常量："GENERIC" / "PIG"
├── RelicPools/
│   └── HextechPigRunePool.cs          # 猪猪符文遗物池
├── Relics/
│   ├── HextechPigRuneBase.cs          # 猪猪专属符文基类（HextechPigRunePool）
│   ├── HextechSharedRuneBase.cs       # 共享符文基类（SharedRelicPool）
│   ├── HextechPigForgeBase.cs         # 猪猪锻造基类（复用海克斯图标）
│   ├── PigletDashRune.cs ~ PerpetualPigRune.cs   # 15 个猪猪符文
│   ├── SavingsAccountRune.cs, HeartyMealRune.cs       # 2 个共享符文
│   ├── SinOfGluttonyRune.cs ~ SinOfWrathRune.cs   # 7 个七宗罪共享符文
│   └── PigletCollarForge.cs           # 1 个锻造
└── Powers/
    └── HextechPigletGuardMinionPower.cs
```

### 内容统计

| 类别 | 数量 | 基类 | 注册池 |
|------|------|------|--------|
| 猪猪专属符文 | 15（6 Silver + 5 Gold + 4 Prismatic） | `HextechPigRuneBase` | `HextechPigRunePool` |
| 共享符文 | 9（2 Silver + 7 Gold·七宗罪） | `HextechSharedRuneBase` | `SharedRelicPool` |
| 猪猪锻造 | 1（Silver） | `HextechPigForgeBase` | `HextechPigRunePool` |
| 辅助 Power | 1 | `YuWanPowerModel` | `[Pool]` |

### 特殊设计点

- **三层 Harmony 注入**：目录注入（13 个方法）、运行时识别范围（6 个方法）、图鉴兼容（3 个方法）
- **`AsyncLocal<int>` 识别范围**：通过 Prefix/Postfix 标记"正在扫描拥有符文"的调用栈范围，确保猪猪符文在该范围内被正确识别
- **七宗罪指环联动**：`HextechPigRuneSharedState` 提供 `ScaleWithRingBonus` 和 `RollPercent` 两个共享方法，多个符文统一使用
- **互斥规则**：泛型方法 `AddMutualBlock<TA, TB>` 减少重复代码

### 具体符文效果速查

详见 `HextechPigRuneRegistry.cs` 和 `HextechForgeRegistry.cs` 中的类型列表，以及各 `.cs` 文件中的 `CanonicalVars` 和生命周期方法。

## 案例：你画瓦猜模组联动

你画瓦猜（DrawAndGuessMod）是第二个基于此架构实现的联动。与海克斯的「注入整套遗物内容」不同，它属于**轻量 UI 注入**——只往对方的作画工具栏追加一个内容项，不新增任何遗物/卡牌。

### 目录结构

```
YuWanCardCode/Integrations/DrawAndGuess/
└── DrawAndGuessRuntimeCompat.cs       # 入口 + 单一 Postfix 注入（无子目录）
```

### 内容：小猪印花

- 在你画瓦猜的作画工具栏中追加小猪作为**第六个角色印花**（原版有 5 个：铁甲战士～摄政王，序号 0–4）。
- 印花贴图固定为 `res://YuWanCard/images/characters/character_icon_pig.png`，**不随小猪皮肤切换**。
- 序号取 5：对方 `DrawingCommand` 将 `StampIndex` 按 3 bit 序列化（0–7），不与原版冲突；未安装 YuWanCard 的多人客户端没有序号 5 的印花，相关笔迹被静默跳过，安全降级。

### 实现要点

1. **复用对方的私有方法**：Postfix `DrawingScreen.AddStampButton`，在最后一个原版印花（`stampIndex == 4`）之后，通过反射调用对方私有的 `AddStampButton(tools, pig, (byte)5)`，按钮、tooltip、工具切换、尺寸控制与原版印花完全一致。
2. **固定贴图覆盖**：反射读取 `DrawingScreen._canvas` 字段并调用公开的 `DrawingCanvas.RegisterStamp(5, fixedTexture)` 覆盖印花图像，同时把新加按钮的 `Icon` 替换为同一张固定贴图。
   - 若直接传 Pig 角色给 `AddStampButton`，其 `character.IconTexture` 会经过本项目的 `IconTexturePathPatch` 路由到 `CustomIconTexturePath`（随皮肤），因此必须主动覆盖。
3. **锚点耦合**：以 `stampIndex == 4` 作为插入锚点，对方若升级原版印花数量会静默降级（小猪印花不出现）而非崩溃。

### 接入点

`MainFile.Initialize` 阶段 2 中 `patcher.ApplySingle(DrawAndGuessRuntimeCompat.TryInstall, ...)`，并在 `NMainMenu._Ready` / `NGame._Ready` 补丁中调用 `TryInstallIfAvailable()` 兜底。

### 更多联动机会（未实现）

- 卡牌识别候选、遗物鉴定事件（`RelicAppraisalFair`）、设置页卡池检测均已因对方遍历全部已加载池而**自动生效**，无需代码。
- 潜在方向：小猪专属「瓦库画笔」遗物（开局送空白，需 ModCompat 门控）、篝火「给小猪画像」事件（复用对方画布）、更多猪猪贴纸（占用序号 6、7）。

## 快速检查清单

新联动上线前确认：

- [ ] 目标模组 ID 正确，与 `manifest.json` 中的 `id` 一致
- [ ] `TryCreate` 返回 `null` 时静默跳过（不抛异常、不报错）
- [ ] 用 `_installed` 标记防重复安装
- [ ] 基类正确设置 `Rarity = RelicRarity.None`（如果需要目标模组管理稀有度）
- [ ] 图标路径与基类 `IconBasePath` 一致，图片已放置
- [ ] 本地化 key 已在 `relics.json` / `powers.json` 中添加
- [ ] `relic_collection.json` 中添加了图鉴分类（如需要）
- [ ] 目标类型 `ResolveType` 失败时有 Warn 日志
- [ ] 非关键 Patch 失败时不阻断初始化（如图鉴显示 Patch）
- [ ] 互斥规则正确实现（如有）
- [ ] 共享内容注册在 `SharedRelicPool`，不污染猪猪专属池
