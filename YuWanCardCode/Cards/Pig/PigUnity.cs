using YuWanCard.Core.Abstracts;
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
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6);
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

        bool hasLivingPig = CombatState!.Allies.Any(c => c.IsAlive && c.Monster is YuWanCard.Monsters.PigMinion);
        bool hasOtherLivingPlayer = CombatState.Players.Any(p => p.Creature != Owner.Creature && !p.Creature.IsDead);

        if (hasLivingPig || hasOtherLivingPlayer)
        {
            foreach (var teammate in CombatState.Players.Select(p => p.Creature).Where(c => c is { IsAlive: true }))
            {
                await PowerCmd.Apply<StrengthPower>(teammate, DynamicVars.Strength.IntValue, Owner.Creature, this);
            }
        }
    }
}
