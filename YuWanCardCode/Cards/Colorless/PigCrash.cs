using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigCrash : YuWanCardModel
{
    public PigCrash() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AllEnemies)
    {
        WithDamage(14);
        WithVar("SelfDamage", 2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["SelfDamage"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature);
        
        if (CombatState != null)
        {
            var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
            foreach (var enemy in enemies)
            {
                VfxUtils.PlayAtCreature("res://YuWanCard/scenes/vfx/vfx_pig_crash.tscn", enemy);
            }
        }
        
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
    }
}
