using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class Nyjk : YuWanCardModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Nyjk() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy
    )
    {
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

    public override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}
