using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class TurnToSpecimen : YuWanCardModel
{
    public TurnToSpecimen() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllEnemies)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        foreach (var enemy in CombatState.Enemies)
        {
            var powersToRemove = enemy.Powers.Where(p => p.Type == PowerType.Buff).ToList();

            foreach (var buff in powersToRemove)
            {
                await PowerCmd.Remove(buff);
            }

            // 将敌人变成灰白色，停止动画，持续 3 秒
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(enemy);
            MainFile.Logger.Debug($"TurnToSpecimen: creatureNode = {creatureNode != null}");
            if (creatureNode != null)
            {
                // 使用 Body 节点来应用颜色效果
                var body = creatureNode.Body;
                MainFile.Logger.Debug($"TurnToSpecimen: body = {body != null}");
                if (body != null)
                {
                    // 保存原始颜色
                    var originalColor = body.Modulate;

                    // 渐变到灰白色
                    var tween = creatureNode.CreateTween();
                    tween.TweenProperty(body, "modulate", new Color(0.7f, 0.7f, 0.7f, 1.0f), 0.3f)
                        .SetEase(Tween.EaseType.InOut);

                    // 停止动画
                    if (creatureNode.HasSpineAnimation)
                    {
                        // 设置为 idle 动画并暂停
                        creatureNode.SpineAnimation.SetAnimation("idle", loop: false, 0);
                        creatureNode.SpineAnimation.SetTimeScale(0f);
                    }

                    // 3 秒后恢复原始颜色和动画
                    tween.TweenInterval(2.7f);
                    tween.TweenProperty(body, "modulate", originalColor, 0.5f)
                        .SetEase(Tween.EaseType.InOut);

                    // 恢复动画
                    tween.TweenCallback(Callable.From(() =>
                    {
                        if (creatureNode.HasSpineAnimation)
                        {
                            creatureNode.SpineAnimation.SetTimeScale(1f);
                            creatureNode.SpineAnimation.SetAnimation("idle", loop: true, 0);
                        }
                    }));
                }
            }
        }

        await Task.CompletedTask;
    }
}
