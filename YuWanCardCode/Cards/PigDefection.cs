using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigDefection : YuWanCardModel
{
    private const int DefectionChance = 40;

    public PigDefection() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.RandomEnemy)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        var rng = Owner.RunState?.Rng?.Niche;
        if (rng == null) return;

        var enemies = CombatState.HittableEnemies;
        if (enemies.Count == 0) return;

        var target = cardPlay.Target;
        if (target == null || target.IsDead)
        {
            target = rng.NextItem(enemies);
        }

        if (target == null) return;

        bool success = rng.NextInt(100) < DefectionChance;

        if (success)
        {
            await PetManager.DefectEnemyToPet(Owner, target);
        }
        else
        {
            MainFile.Logger.Info($"PigDefection failed for {target.Name}");
        }
    }
}
