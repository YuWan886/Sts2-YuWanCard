using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class SadArmyWin : YuWanCardModel
{
    public SadArmyWin() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var maxHp = Owner.Creature.MaxHp;
        var currentHp = Owner.Creature.CurrentHp;
        
        if (maxHp <= 0) return;
        
        var healthPercent = (float)currentHp / maxHp;
        
        if (healthPercent <= 0.1f)
        {
            if (IsUpgraded)
            {
                var allEnemies = CombatState!.Enemies.ToList();
                foreach (var enemy in allEnemies)
                {
                    if (enemy.IsAlive)
                    {
                        await KillEnemy(choiceContext, enemy);
                    }
                }
            }
            else
            {
                if (cardPlay.Target != null && cardPlay.Target.IsAlive)
                {
                    await KillEnemy(choiceContext, cardPlay.Target);
                }
            }
        }
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private async Task KillEnemy(PlayerChoiceContext choiceContext, Creature enemy)
    {
        await CreatureCmd.Damage(choiceContext, enemy, enemy.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature);
    }
}
