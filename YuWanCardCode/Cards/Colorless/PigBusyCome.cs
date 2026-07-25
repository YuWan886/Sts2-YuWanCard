using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigBusyCome : YuWanCardModel
{
    public PigBusyCome() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<SlothPower>(4);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardsToPlay = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this)
            .ToList();

        foreach (var card in cardsToPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding)
                break;

            await CardCmd.AutoPlay(choiceContext, card, null);
        }

        await PowerCmd.Apply<SlothPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["SlothPower"].BaseValue, Owner.Creature, this);
    }
}
