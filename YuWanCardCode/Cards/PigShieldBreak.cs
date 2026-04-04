using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigShieldBreak : YuWanCardModel
{
    public PigShieldBreak() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Token,
        target: TargetType.AnyEnemy)
    {
        WithDamage(5);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            int blockToRemove = cardPlay.Target.Block / 2;
            if (blockToRemove > 0)
            {
                await CreatureCmd.LoseBlock(cardPlay.Target, blockToRemove);
            }
        }
        await CommonActions.CardAttack(this, cardPlay, hitCount: 1).Execute(choiceContext);
    }
}
