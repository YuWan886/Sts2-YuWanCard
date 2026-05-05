using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Orbs;

namespace YuWanCard.Cards.Colorless;

[Pool(typeof(ColorlessCardPool))]
public class LittleSnakeBite : YuWanCardModel
{
    public LittleSnakeBite() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Retain);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromOrb<SnakeBiteOrb>()));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = IsUpgraded ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            await OrbCmd.Channel<SnakeBiteOrb>(choiceContext, Owner);
        }
    }
}
