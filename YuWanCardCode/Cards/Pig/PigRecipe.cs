using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigRecipe : YuWanCardModel
{
    public PigRecipe() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithVar("FoodCount", 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FoodCount"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        for (int index = 0; index < DynamicVars["FoodCount"].IntValue; index++)
        {
            var foodCard = CombatState.CreateCard(CardUtils.GetRandomFoodPigCardCanonical(Owner), Owner);
            await CardPileCmd.AddGeneratedCardToCombat(foodCard, PileType.Hand, Owner);
        }
    }
}
