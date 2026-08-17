using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigRicochet : YuWanCardModel
{
    public PigRicochet() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllEnemies)
    {
        WithDamage(9, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        var enemies = CombatState.Enemies.Where(enemy => enemy.IsAlive).ToList();
        foreach (var enemy in enemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override PileType GetResultPileTypeForCardPlay()
    {
        PileType resultPileType = base.GetResultPileTypeForCardPlay();
        if (resultPileType != PileType.Discard)
        {
            return resultPileType;
        }
        return PileType.Hand;
    }
}
