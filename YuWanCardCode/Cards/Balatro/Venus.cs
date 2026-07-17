using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Venus : YuWanCardModel
{
    public Venus() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithPower<VenusPower>("BonusBlock", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VenusPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["BonusBlock"].BaseValue, Owner.Creature, this);
    }
}
