# 版本兼容性

## 多版本兼容加载机制

本项目采用与 **STS2-RitsuLib** 相同的「loader + 多版本变体」机制：一个小的 loader 程序集作为游戏加载入口，运行时按宿主游戏版本挑选并加载对应的内容变体 DLL。

```
mods/YuWanCard/
├─ YuWanCard.dll                  ← loader（游戏入口，带 [ModInitializer]）
├─ YuWanCard.json                 ← mod 清单（id=YuWanCard，min_game_version）
├─ YuWanCard.pck                  ← Godot 资源包（各变体共享）
├─ yuwan-variants.manifest        ← 变体清单
├─ posthog.analytics.local.yaml   ← 本地分析配置
└─ lib/
   ├─ 0.107.1/compat-target.txt + YuWanCard.Content.dll
   └─ 0.110.1/compat-target.txt + YuWanCard.Content.dll
```

### 为什么需要它

游戏大版本更新时，`sts2.dll` 的 API 可能变化（例如 0.110.x 给伤害修正 Hook 增加了
`CardPlay` 参数、把 `BeforeTurnEnd` 改名为 `BeforeSideTurnEnd` 等）。内容程序集按某个
游戏版本编译后，在另一个版本上可能无法加载。loader 机制让**一个 mod 安装包同时覆盖
多个游戏版本**：每个游戏版本一个内容变体，运行时选「≤ 宿主版本的最新变体」。

### 运行时加载流程（`YuWanCardCode/Loader/`）

1. 游戏加载 `mods/YuWanCard/YuWanCard.dll`（loader，带 `[ModInitializer]`）。
2. `LoaderMain.Initialize()` 先安装两个 Harmony 桥：
   - `ReflectionHelperModTypesPatch`：把已加载的内容变体类型追加到
     `ReflectionHelper.ModTypes`，这样游戏的 `ModelDb` / `ActionTypes` /
     `MessageTypes` 才能发现变体里的模型（游戏只扫 `mod.assembly` = loader）。
   - `HarmonyPatchAllTypeLoadGuard`：`PatchAll` 遇到加载不了的 patch 类型时跳过而非中断。
3. `LoaderHostVersion` 解析宿主版本（主源 `ReleaseInfoManager`，回退
   `release_info.json` / Godot 数据目录 `.cache_stamp` / `SerializableRun` 程序集版本）。
4. `LoaderVariantBundle` 读取 `yuwan-variants.manifest`，校验每个变体
   （sha256 + `compat-target.txt` + 目录名），选出 `compatTarget <= 宿主` 的最新变体；
   无匹配则用最新变体兜底。
5. 用 `AssemblyLoadContext.LoadFromAssemblyPath` 载入
   `lib/<version>/YuWanCard.Content.dll` 到默认 ALC（与游戏程序集绑定）。
6. 反射扫描内容变体内的 `[ModInitializer]` 类型并调用其静态 `Initialize()`。
   内容代码里 `Assembly.GetExecutingAssembly()` 因此拿到的是内容程序集本身。

**Loader 只允许引用极稳定的游戏 API**（`ModInitializerAttribute`、`Logger`、
`ReleaseInfoManager`、`ReflectionHelper`、`ModManager`、Harmony），并尽量按**最老**支持
版本编译，以保证它能跨版本加载。不要在这里引用易变 API。

### 内容侧注意事项

- 内容程序集名改为 `YuWanCard.Content`（命名空间仍为 `YuWanCard`）。
- `AssetPathHelper` 的 `res://YuWanCard/...` 路径改为常量，不再从程序集名推导。
- `MainFile.ModRootDir` 向上回溯定位 mod 根目录（内容 DLL 现位于 `lib/<version>/`）。
- 内容 DLL 通常位于 `lib/<version>/` 下，`UpdateChecker` / `CloudAnalyticsService`
  通过 `MainFile.ModRootDir` 找到 mod 根目录里的 `YuWanCard.json` 与
  `posthog.analytics.local.yaml`。

## 构建与发布

### 开发循环（单变体）

```bash
dotnet build
```

会同时编译 loader（ProjectReference）与内容，并把当前游戏版本的内容部署到
`mods/YuWanCard/lib/<当前版本>/`，重生成 `yuwan-variants.manifest`。版本号取自
`$(Sts2DataDir)/../release_info.json`，也可用 `/p:VariantTarget=0.107.1` 显式指定。

### 多版本构建

需要每个游戏版本的 `sts2.dll` 快照，建议目录布局（每个版本一个子目录，含 `sts2.dll`）：

```
F:\sts2-mod\sts2-versions\
├─ 0.107.1\sts2.dll
└─ 0.110.1\sts2.dll
```

```powershell
powershell -ExecutionPolicy Bypass -File tools/build-variants.ps1 -ApiRoot F:\sts2-mod\sts2-versions
```

脚本会：
1. 用**最老**快照编译 loader；
2. 对每个快照编译内容变体（`/p:Sts2DataDir=<快照临时目录>` + `/p:VariantTarget=<版本>`），
   逐个部署到 `mods/YuWanCard/lib/<版本>/`；
3. 重生成 `yuwan-variants.manifest`（含 sha256）；
4. 结束后清理临时目录。

可用 `-Versions 0.107.1` 只构建指定版本。构建期间会跳过 PCK 打包（PCK 与版本无关，
用正常的 `dotnet build` 生成）。

### 支持新游戏版本

1. 获取该版本的 `sts2.dll`，放进 `sts2-versions/<版本>/sts2.dll`。
2. 把内容代码适配到新 API（改动签名、重命名 Hook 等）。由于单程序集只能 override 一种
   签名，若要同时支持两个版本的差异 API，需要在相关文件用条件编译
   （如 `#if YUWANCARD_COMPAT_110`）区分。**建议默认只维护当前游戏版本**，游戏大更新时
   一次性适配并切换。
3. 运行 `build-variants.ps1` 生成/更新变体。

### 游戏版本历史

| 游戏版本 | 模组变体 | 说明 |
|----------|----------|------|
| 0.107.1 | 有（历史兼容变体） | 内容使用 0.107.1 的 Hook 签名，供老版本回退 |
| 0.110.1 | 有（当前发布） | 已适配 0.110 API：伤害 Hook 增加 `CardPlay` 参数、`BeforeTurnEnd/AfterTurnEnd` → `BeforeSideTurnEnd/AfterSideTurnEnd`、`ModifyCardPlayResultPileTypeAndPosition` → `ModifyCardPlayResultLocation`、移除 `GetResultPileTypeForCardPlay`、`EpochModel.IsArtPlaceholder` 变更 |

> 当前宿主游戏为 0.110.1，`min_game_version=0.110.0`。开发循环（`dotnet build`）读取
> `release_info.json` 自动生成 `lib/0.110.1` 变体；loader 运行时按「compatTarget ≤ 宿主」
> 挑选变体，0.107.1 变体供 0.108 及更老版本回退。

---

## 平台兼容性

### 支持的平台

| 平台 | 支持状态 | 注意事项 |
|------|----------|----------|
| Windows | 完全支持 | 主要开发平台 |
| Linux | 支持 | 路径使用 `/` 分隔符 |
| macOS | 支持 | 路径使用 `/` 分隔符 |
| Android | 支持 | 注意本地化前缀回退 |
| iOS | 支持 | 注意本地化前缀回退 |

### 平台检测

游戏本身会自动处理平台差异。对于 Android 平台，`LocalizationPrefixFallbackPatch` 自动处理本地化前缀丢失问题。

### 移动端特殊处理

Android 平台存在本地化前缀丢失的问题，项目通过 `LocalizationPrefixFallbackPatch` 自动处理：

```csharp
// 在 LocTable.GetRawText 抛出 LocException 时
// 自动重试添加 YUWANCARD- 前缀的键查找
```

---

## 依赖兼容性

### 核心依赖

| 依赖 | 版本 | 说明 |
|------|------|------|
| .NET | 9.0 | 目标框架 |
| Godot | 4.5.1 | 游戏引擎 |
| Alchyr.Sts2.ModAnalyzers | 最新 | 模组注册源码生成器 |
| Krafs.Publicizer | 最新 | 内部成员访问 |
| BSchneppe.StS2.PckPacker | 最新 | PCK 打包 |

### 可选依赖

| 依赖 | 说明 |
|------|------|
| RitsuLib | 配置系统兼容 |

配置页面通过运行时反射注册到 `STS2RitsuLib`，无需编译时引用：

```csharp
internal static class ConfigRegistrar
{
    public static void TryDeferredRegister() { }
}
```
