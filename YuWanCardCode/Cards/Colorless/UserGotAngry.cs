using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class UserGotAngry : YuWanCardModel
{
    public UserGotAngry() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithTip(typeof(Anger));
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        var anger = CombatState.CreateCard<Anger>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(anger);

        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(anger, PileType.Hand, Owner),
            2f);
    }
}
