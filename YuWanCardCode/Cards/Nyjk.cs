using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class Nyjk : YuWanCardModel
{
    public Nyjk() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    public override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            int blockToRemove = cardPlay.Target.Block / 2;
            if (blockToRemove > 0)
            {
                await CreatureCmd.LoseBlock(cardPlay.Target, blockToRemove);
            }
            await PowerCmd.Apply<WeakPower>(cardPlay.Target, 1, Owner.Creature, this);
        }
    }
}
