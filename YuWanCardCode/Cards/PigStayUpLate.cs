using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigStayUpLate : YuWanCardModel
{
    public PigStayUpLate() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithDamage(9);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            decimal damage = DynamicVars.Damage.BaseValue;
            
            if (IsLateNight())
            {
                damage *= 2;
                await PowerCmd.Apply<WeakPower>(Owner.Creature, 1, Owner.Creature, this);
                MainFile.Logger.Info($"PigStayUpLate: Late night bonus! Damage doubled to {damage}, gained Weak");
            }
            
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    private static bool IsLateNight()
    {
        var now = DateTime.UtcNow;
        int hour = now.Hour;
        return hour >= 23 || hour < 2;
    }
}
