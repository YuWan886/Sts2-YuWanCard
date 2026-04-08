using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigUnity : YuWanCardModel
{
    public PigUnity() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithDamage(4);
        WithPower<StrengthPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        var teammates = CombatState!.GetTeammatesOf(Owner.Creature);
        int aliveTeammates = teammates.Count(t => t.IsAlive);

        if (aliveTeammates > 0)
        {
            foreach (var teammate in teammates.Where(t => t.IsAlive))
            {
                await PowerCmd.Apply<StrengthPower>(teammate, DynamicVars.Strength.IntValue, Owner.Creature, this);
            }
        }
    }
}
