using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigNineLegs : YuWanCardModel
{
    public PigNineLegs() : base(
        baseCost: 3,
        type: CardType.Attack,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy)
    {
        WithDamage(29);
        WithPower<StranglePower>(9);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!attackCommand.Results.Any(result => result.WasTargetKilled) || CombatState == null)
            return;

        var otherEnemies = CombatState.HittableEnemies
            .Where(enemy => enemy != cardPlay.Target)
            .ToList();

        if (otherEnemies.Count > 0)
        {
            await PowerCmd.Apply<StranglePower>(
                otherEnemies,
                DynamicVars["StranglePower"].IntValue,
                Owner.Creature,
                this);
        }
    }
}
