# 项目设置

## 引用 BaseLib

1. 将 BaseLib 项目添加到你的解决方案中
2. 在你的模组项目中添加对 BaseLib 的引用
3. 确保你的模组的 `mod_manifest.json` 文件中包含 BaseLib 作为依赖

```json
{
  "id": "YourModId",
  "name": "Your Mod Name",
  "version": "1.0.0",
  "dependencies": [
    {
      "id": "BaseLib",
      "version": "1.0.0"
    }
  ]
}
```

## 基本结构

推荐的项目结构：

```
YourMod/
├── Abstracts/         # 自定义抽象类（如果需要）
├── Ancients/          # 先古之民事件定义
├── Cards/             # 卡牌定义
├── Characters/        # 角色定义
├── Modifiers/         # 修改器定义（如无尽模式）
├── Relics/            # 遗物定义
├── Powers/            # 能力定义
├── Potions/           # 药水定义
├── Config/            # 配置相关
├── Patches/           # Harmony 补丁
├── Utils/             # 工具类
├── MainFile.cs        # 模组入口
├── YourMod.json       # 模组元数据
└── project.godot      # Godot 项目文件

YourMod/               # 模组资源目录
├── images/
│   ├── card_portraits/    # 卡牌立绘
│   ├── powers/            # 能力图标
│   ├── relics/            # 遗物图标
│   ├── ancients/          # 先古之民图标和背景
│   ├── modifiers/         # 修改器图标
│   └── ui/run_history/    # UI 图标
├── scenes/
│   └── ancients/          # 先古之民场景
├── localization/
│   ├── zhs/               # 简体中文
│   │   ├── cards.json
│   │   ├── powers.json
│   │   ├── relics.json
│   │   ├── ancients.json
│   │   └── modifiers.json
│   └── eng/               # 英文
└── mod_image.png          # 模组图标
```

## PoolAttribute 属性

BaseLib 使用 `PoolAttribute` 属性来确定自定义内容应该添加到哪个池中。所有继承自 `ICustomModel` 的自定义模型都需要使用此属性。

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

[Pool(typeof(SharedRelicPool))]
public class MyCustomRelic : CustomRelicModel
{
}
```

常用的池类型：
- **卡牌池**：`SharedCardPool`、`IroncladCardPool`、`SilentCardPool`、`DefectCardPool`、`RegentCardPool`、`NecrobinderCardPool`、`ColorlessCardPool`（无色卡牌）、`TokenCardPool`、`EventCardPool`、`QuestCardPool`、`StatusCardPool`、`CurseCardPool`
- **遗物池**：`SharedRelicPool`、`IroncladRelicPool`、`SilentRelicPool`、`DefectRelicPool`、`RegentRelicPool`、`NecrobinderRelicPool`、`EventRelicPool`
- **药水池**：`SharedPotionPool`、`IroncladPotionPool`、`SilentPotionPool`、`DefectPotionPool`、`RegentPotionPool`、`NecrobinderPotionPool`、`EventPotionPool`、`TokenPotionPool`

**注意**：使用卡牌池类型时需要引入命名空间 `MegaCrit.Sts2.Core.Models.CardPools`。
