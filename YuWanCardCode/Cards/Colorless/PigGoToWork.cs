using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigGoToWork : YuWanCardModel
{
    public PigGoToWork() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.None)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
        WithTip(typeof(PigTouchFish));
        WithTip(typeof(PigOffWork));
        WithTip(typeof(PigOvertime));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainGold(15, Owner);

        var touchFish = CombatState!.CreateCard(ModelDb.Card<PigTouchFish>(), Owner);
        var offWork = CombatState.CreateCard(ModelDb.Card<PigOffWork>(), Owner);
        var overtime = CombatState.CreateCard(ModelDb.Card<PigOvertime>(), Owner);

        await CardPileCmd.AddGeneratedCardToCombat(touchFish, PileType.Hand, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(offWork, PileType.Discard, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(overtime, PileType.Draw, Owner);
    }
}
