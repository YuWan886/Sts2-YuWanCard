using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Mercury : YuWanCardModel
{
    public Mercury() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null)
        {
            return;
        }

        int bonus = IsUpgraded ? 2 : 1;
        foreach (CardModel card in combatState.AllCards.Where(card => card.Type == CardType.Attack))
        {
            if (card.DynamicVars.ContainsKey("Damage"))
            {
                card.DynamicVars["Damage"].BaseValue += bonus;
            }
        }

        await Task.CompletedTask;
    }
}
