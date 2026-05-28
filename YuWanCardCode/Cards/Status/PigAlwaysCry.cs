using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(StatusCardPool))]
public class PigAlwaysCry : YuWanCardModel
{
    public override int MaxUpgradeLevel => 0;

    public PigAlwaysCry() : base(
        baseCost: -2,
        type: CardType.Status,
        rarity: CardRarity.Status,
        target: TargetType.None,
        showInCardLibrary: false)
    {
        WithKeywords(CardKeyword.Ethereal, CardKeyword.Unplayable);
    }
}
