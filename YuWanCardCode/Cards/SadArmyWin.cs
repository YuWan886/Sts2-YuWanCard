using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class SadArmyWin : YuWanCardModel
{
    public SadArmyWin() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var maxHp = Owner.Creature.MaxHp;
        var currentHp = Owner.Creature.CurrentHp;
        
        if (maxHp <= 0) return;
        
        var healthPercent = (float)currentHp / maxHp;
        
        if (healthPercent <= 0.1f)
        {
            var primaryEnemies = CombatState!.Enemies
                .Where(e => e.IsAlive && e.IsPrimaryEnemy)
                .ToList();

            foreach (var enemy in primaryEnemies)
            {
                await KillEnemy(choiceContext, enemy, Owner.Creature);
            }
        }
    }

    private static async Task KillEnemy(PlayerChoiceContext choiceContext, Creature enemy, Creature source)
    {
        await CreatureCmd.Damage(choiceContext, enemy, enemy.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered, source);
    }
}
