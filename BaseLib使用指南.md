# BaseLib 使用指南

BaseLib 是一个为 Slay the Spire 2 (StS2) 模组开发提供基础功能的库，它简化了模组开发过程，提供了各种抽象类和工具来帮助开发者创建自定义内容。

## 文档目录

### 入门指南

1. [项目设置](docs/01-project-setup.md) - 如何设置项目、引用 BaseLib、项目结构
2. [核心功能](docs/02-core-features.md) - 卡牌、角色、遗物、能力、药水、先古之民等核心功能
3. [配置系统](docs/03-config-system.md) - 模组配置和 SavedProperty 属性

### 进阶功能

4. [自定义 Modifier](docs/04-custom-modifier.md) - 创建自定义游戏模式修改器
5. [工具类](docs/05-utils.md) - GodotUtils、CommonActions、ModelDb 等工具
6. [自定义动态变量](docs/06-custom-variables.md) - PersistVar、RefundVar、ExhaustiveVar

### 参考资料

7. [最佳实践](docs/07-best-practices.md) - 命名约定、调试、性能优化
8. [示例代码](docs/08-examples.md) - 完整的模组示例代码
9. [故障排除](docs/09-troubleshooting.md) - 常见问题和解决方案
10. [扩展功能](docs/10-extensions.md) - 自定义变量、遗物升级等扩展功能

## 快速开始

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(ColorlessCardPool))]
public class MyFirstCard : CustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m)];

    public MyFirstCard() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.Enemy
    )
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attackCmd = CommonActions.CardAttack(this, cardPlay, hitCount: 1);
        await choiceContext.RunCommand(attackCmd);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

## 核心概念

- **CustomCardModel**：自定义卡牌基类
- **CustomCharacterModel**：自定义角色基类
- **CustomRelicModel**：自定义遗物基类
- **CustomPowerModel**：自定义能力基类
- **CustomPotionModel**：自定义药水基类
- **CustomAncientModel**：自定义先古之民基类
- **PoolAttribute**：内容池属性标记
- **CommonActions**：常用游戏动作工具
- **ModelDb**：游戏模型数据库

## 相关链接

- [BaseLib 项目](https://github.com/Alchyr/BaseLib-StS2)
