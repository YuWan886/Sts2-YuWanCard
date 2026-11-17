using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigUnity : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigUnity() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithPower<StrengthPower>(1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, null)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        bool hasLivingPig = CombatState!.Allies.Any(c => c.IsAlive && c.Monster is YuWanCard.Monsters.PigMinion);
        var livingPlayers = CombatState.GetLivingPlayerCreatures();
        bool hasOtherLivingPlayer = livingPlayers.Any(c => c != Owner.Creature);

        if (hasLivingPig || hasOtherLivingPlayer)
        {
            foreach (var teammate in livingPlayers)
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),teammate, DynamicVars.Strength.IntValue, Owner.Creature, this);
            }
        }
    }
}
