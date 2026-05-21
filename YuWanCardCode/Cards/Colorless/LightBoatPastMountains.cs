using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class LightBoatPastMountains : YuWanCardModel
{
    public override int MaxUpgradeLevel => 0;
    public override bool UseAncientVisualStyle => true;

    public LightBoatPastMountains() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var cardsToPlay = exhaustPile.Cards
            .Where(card => card is not LightBoatPastMountains)
            .ToList();

        foreach (var card in cardsToPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding)
                break;

            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }
}
