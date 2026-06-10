using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;
using YuWanCard.Modifiers;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Dividend : YuWanCardModel
{
    public Dividend() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        BalatroModifier? modifier = Owner.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
        if (modifier == null)
        {
            return;
        }

        int gold = (int)Math.Floor(modifier.GetComboCounter(Owner) / 5f) * 3;
        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, Owner);
        }
    }
}
