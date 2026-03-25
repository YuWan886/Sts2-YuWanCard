# 故障排除

## 常见问题

### 1. 卡牌不显示

**原因**：卡牌未正确注册到卡牌池

**解决方案**：
- 确保设置了 `showInCardLibrary: true`
- 确保设置了 `autoAdd: true`
- 确保使用了正确的 `PoolAttribute`
- 检查卡牌池类型是否正确

```csharp
[Pool(typeof(ColorlessCardPool))]
public class MyCard : CustomCardModel
{
    public MyCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.Enemy,
        showInCardLibrary: true,
        autoAdd: true
    )
    {
    }
}
```

### 2. 角色不显示

**原因**：视觉场景未正确配置

**解决方案**：
- 确保设置了正确的 `CustomVisualPath`
- 或者在 `res://scenes/creature_visuals/` 目录下创建对应名称的场景文件
- 检查场景中是否包含必要的节点：`Visuals`、`Bounds`、`IntentPos`、`CenterPos`、`OrbPos`、`TalkPos`

### 3. 配置不生效

**原因**：配置属性未正确设置

**解决方案**：
- 确保配置属性是**静态属性**（`static`）
- 确保配置属性有 `get` 和 `set` 访问器
- 确保在 `SetupConfigUI` 方法中正确添加了配置选项

### 4. Harmony 补丁失败

**原因**：目标方法不存在或签名不匹配

**解决方案**：
- 检查补丁代码是否正确
- 确保目标方法存在
- 使用 `MainFile.Logger` 输出调试信息

### 5. 自定义视觉不加载

**原因**：场景文件或路径问题

**解决方案**：
- 确保场景文件存在且路径正确
- 检查场景中是否包含必要的节点
- 确保 `.pck` 文件正确导出

### 6. 卡牌池不显示

**原因**：卡牌池未正确注册

**解决方案**：
- 确保正确设置了 `IsShared` 属性
- 角色卡牌池需要通过角色的 `CardPool` 属性引用
- 共享卡牌池会自动注册到 `ModelDb.AllSharedCardPools`

### 7. 配置文件不保存

**原因**：权限或路径问题

**解决方案**：
- 确保模组有写入权限
- 检查文件路径是否正确

### 8. PoolAttribute 错误

**原因**：池类型不匹配

**解决方案**：
- 确保所有自定义模型都使用了 `PoolAttribute`
- 确保池类型与模型类型匹配

### 9. SavedProperty 不保存

**原因**：属性未正确标记或命名冲突

**解决方案**：
- 确保属性使用了 `[SavedProperty]` 特性
- 确保属性是公共的，且有 `get` 和 `set` 访问器
- BaseLib 会自动处理 SavedProperty 的注册
- 建议为属性名添加前缀以避免命名冲突

### 10. Modifier 不生效

**原因**：未正确注册到 ModelDb

**解决方案**：
- 确保 Modifier 已通过 Harmony 补丁注册到 `ModelDb`
- 确保 Modifier 已添加到 `GoodModifiers` 列表
- 参考 [自定义 Modifier](04-custom-modifier.md) 中的注册代码

## 日志

查看游戏日志来排查问题。

**游戏日志位置**：
- Windows: `C:\Users\[用户名]\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs/godot.log`

**日志级别**：
- `Info`：重要操作（初始化、保存、加载）
- `Debug`：详细调试信息
- `Warn`：警告信息
- `Error`：错误信息

### BaseLib 日志窗口

BaseLib 提供了一个内置的日志窗口，方便实时查看游戏日志。

**打开日志窗口**：
1. **控制台命令**：在游戏内按 `~` 键打开控制台，输入 `showlog` 命令
2. **启动时自动打开**：在 BaseLib 配置中启用"启动时打开日志窗口"选项

**日志窗口功能**：
- 实时显示游戏日志
- 自动滚动到最新日志
- 不同日志级别使用不同颜色显示：
  - **红色**：ERROR、FATAL、EXCEPTION
  - **黄色**：WARN、WARNING
  - **蓝色**：DEBUG、TRACE、VERYDEBUG
- 可配置日志行数限制（默认 256 行，可在配置中调整）

**配置日志窗口**：

```csharp
// 在你的模组配置中添加
[ConfigSection("日志设置")]
public static bool OpenLogWindowOnStartup { get; set; } = false;

[SliderRange(128, 2048, 64)]
[SliderLabelFormat("{0:0}")]
public static double LimitedLogSize { get; set; } = 256;
```

**通过代码打开日志窗口**：

```csharp
using BaseLib.Patches.Features;

// 在代码中打开日志窗口
OpenLogWindow.OpenWindow();
```

## 调试技巧

1. **使用日志**：使用 `MainFile.Logger.Debug()` 输出详细调试信息

2. **检查 ID 映射**：检查卡牌 ID 和名称映射

3. **测试模组**：在游戏中运行一局，查看模组功能是否正常

4. **检查 Harmony 补丁**：确认补丁是否正确应用

5. **验证文件路径**：确保所有资源路径都是正确的

6. **检查依赖项**：确保 BaseLib 已正确引用

7. **检查 PoolAttribute**：确保池类型与模型类型匹配

8. **检查本地化**：确保本地化键正确

9. **检查多人游戏**：使用 `LocalContext.IsMe(player)` 检查是否为本地玩家

10. **检查空值**：检查 `Owner`、`CombatState` 等是否为 null
