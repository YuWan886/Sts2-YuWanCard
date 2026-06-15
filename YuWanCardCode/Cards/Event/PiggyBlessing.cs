using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards.Event;

[Pool(typeof(EventCardPool))]
public class PiggyBlessing : YuWanCardModel
{
    public PiggyBlessing() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Event,
        target: TargetType.Self)
    {
        WithBlock(6, 3);
        WithCards(1, 1);
        WithTip(new TooltipSource(_ => HoverTipFactory.Static(StaticHoverTip.Block)));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await CommonActions.Draw(this, choiceContext);
    }
}
