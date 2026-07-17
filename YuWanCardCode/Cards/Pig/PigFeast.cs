using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigFeast : YuWanCardModel
{
    public PigFeast() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Rare,
        target: TargetType.AllEnemies)
    {
        WithDamage(8, 2);
        WithVar("RepeatCap", 3, 1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        int repeatCount = Math.Min(
            CardUtils.GetDistinctFoodPigPlayedThisCombat(Owner),
            DynamicVars["RepeatCap"].IntValue);

        int totalHits = 1 + repeatCount;
        var enemies = CombatState.Enemies.Where(enemy => enemy.IsAlive).ToList();
        for (int hit = 0; hit < totalHits; hit++)
        {
            foreach (var enemy in enemies)
            {
                await DamageCmd.Attack(DynamicVars.Damage.IntValue)
                    .FromCard(this)
                    .Targeting(enemy)
                    .WithHitFx("vfx/vfx_bite")
                    .Execute(choiceContext);
            }
        }
    }
}
