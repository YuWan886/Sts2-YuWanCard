using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public sealed class PigLoan : YuWanCardModel
{
    public PigLoan() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVar("GoldGain", 40, 20);
        WithVar("DebtCount", 2, 1);
        WithTip(typeof(Debt));
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null)
        {
            return;
        }

        await PlayerCmd.GainGold(DynamicVars["GoldGain"].IntValue, Owner);

        List<CardPileAddResult> addResults = [];
        int debtCount = DynamicVars["DebtCount"].IntValue;
        for (int i = 0; i < debtCount; i++)
        {
            var debt = CombatState.CreateCard(ModelDb.Card<Debt>(), Owner);
            addResults.Add(await CardPileCmd.AddGeneratedCardToCombat(debt, PileType.Draw, Owner));
        }

        if (addResults.Count > 0)
        {
            CardCmd.PreviewCardPileAdd(addResults);
        }
    }
}
