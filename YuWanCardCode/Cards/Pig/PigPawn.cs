using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPawn : YuWanCardModel
{
    public PigPawn() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: CustomTargetType.AnyPigMinion)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { IsDead: false } pig)
            return;

        if (pig.Monster is not PigMinion)
            return;

        int damage = (int)Math.Ceiling(pig.CurrentHp / 2m);
        if (damage > 0 && CombatState != null)
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await DamageCmd.Attack(damage)
                    .FromCard(this)
                    .Targeting(enemy)
                    .Execute(choiceContext);
            }
        }

        await PetManager.KillPet(pig);
    }
}
