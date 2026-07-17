using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigHerd : YuWanCardModel
{
    public PigHerd() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithVar("BonusDamage", 5, 2);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        int damage = DynamicVars.Damage.IntValue;
        if (HasLivingPig())
        {
            damage += DynamicVars["BonusDamage"].IntValue;
        }

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    private bool HasLivingPig()
    {
        var pig = PetManager.FindPetByType<PigMinion>(Owner.Creature);
        return pig is { IsAlive: true };
    }
}
