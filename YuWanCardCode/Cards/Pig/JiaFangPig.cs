using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public sealed class JiaFangPig : YuWanCardModel
{
    protected override bool IsPlayable =>
        base.IsPlayable && GoldSpendHelper.CanAfford(Owner, DynamicVars.Gold.IntValue);

    protected override bool ShouldGlowRedInHand =>
        Owner != null && Owner.Gold < DynamicVars.Gold.IntValue;

    public JiaFangPig() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithVars(new GoldVar(6));
        WithBlock(9, 4);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await GoldSpendHelper.TrySpend(Owner, DynamicVars.Gold.IntValue, nameof(JiaFangPig)))
        {
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
}
