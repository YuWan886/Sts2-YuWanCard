# 最佳实践

## 命名约定

- 类名：使用 PascalCase
- 方法名：使用 PascalCase
- 属性名：使用 PascalCase
- 字段名：使用 camelCase 或 _camelCase
- 命名空间：使用 PascalCase，通常以模组名称开头

## 组织代码

- 将不同类型的内容放在不同的文件夹中
- 使用命名空间来组织代码
- 保持代码简洁明了
- 使用部分类（partial classes）来组织大型类

## 调试

使用 BaseLib 的日志系统：

```csharp
using BaseLib;

MainFile.Logger.Info("Mod initialized");
MainFile.Logger.Warn("Something might be wrong");
MainFile.Logger.Error("An error occurred");
MainFile.Logger.Debug("Detailed debug information");
```

**日志级别**：
- `Info`：重要操作（初始化、保存、加载）
- `Debug`：详细调试信息（进度计算、卡牌过滤）
- `Warn`：警告信息（卡牌未找到、配置缺失）
- `Error`：错误信息（异常捕获）

**游戏日志位置**：
- Windows: `C:\Users\[用户名]\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs/godot.log`

## 性能

- 避免在游戏循环中做 heavy 操作
- 使用对象池来减少 GC
- 合理使用 Harmony 补丁
- 使用缓存来减少重复计算
- 延迟加载资源和初始化

## 代码规范

- 使用 XML 文档注释（///）为公共 API 添加说明
- 遵循 C# 编码规范
- 保持方法简洁，每个方法只做一件事
- 使用有意义的变量和方法名
- 注释应简洁明了，避免复杂的逻辑描述

## 本地化

- 使用游戏的本地化系统
- 为所有用户可见的文本提供本地化支持
- 遵循游戏的本地化命名约定

**本地化文件格式**：
```json
{
  "MODID-CARD_ID.title": "卡牌名称",
  "MODID-CARD_ID.description": "卡牌描述，支持 {DynamicVar:diff()} 等动态变量",
  "MODID-POWER_ID.title": "能力名称",
  "MODID-POWER_ID.description": "能力描述",
  "MODID-POWER_ID.smartDescription": "能力智能描述"
}
```

**本地化键命名规则**：
- 卡牌：`{ModId}-{CardId}.title` / `.description`
- 能力：`{ModId}-{PowerId}.title` / `.description` / `.smartDescription`
- 遗物：`{ModId}-{RelicId}.title` / `.description` / `.flavor`
- 先古之民：`{ModId}-{AncientId}.title` / `.epithet` / `.pages.{PageName}.description`
- Modifier：`{ModifierId}.title` / `.description` / `.neow_title` / `.neow_description`
- ModId 和 CardId/PowerId 使用大写，用连字符分隔

**描述中的动态变量**：
- `{Damage:diff()}` - 显示伤害值
- `{Block:diff()}` - 显示格挡值
- `{Heal:diff()}` - 显示治疗值
- `{Energy:diff()}` - 显示能量值
- `{Energy:energyIcons()}` - 显示能量图标
- `{PowerName:diff()}` - 显示能力层数
- `{IfUpgraded:show:升级后文本|升级前文本}` - 根据是否升级显示不同文本

**颜色标签**：
- `[gold]文本[/gold]` - 金色文本
- `[red]文本[/red]` - 红色文本
- `[blue]文本[/blue]` - 蓝色文本

## 安全性

- 使用 IL 分析避免赋予怪物专属能力
- 检查 `Owner` 是否为 null
- 使用 `LocalContext.IsMe(player)` 检查是否为本地玩家
- 避免在多人游戏中执行仅限本地的操作

## 多人游戏

- 使用 `CardMultiplayerConstraint` 标记仅限多人的卡牌
- 使用 `LocalContext.IsMe(player)` 检查是否为本地玩家
- 注意命令的执行上下文
