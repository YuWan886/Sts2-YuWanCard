using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class Shan : YuWanCardModel
{
    protected override bool IsPlayable => IntentUtils.AnyEnemyIntendsToAttack(CombatState);

    public Shan() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        if (cardPlay.Target.Monster?.IntendsToAttack != true) return;

        await CreatureCmd.Stun(cardPlay.Target);
    }
}
