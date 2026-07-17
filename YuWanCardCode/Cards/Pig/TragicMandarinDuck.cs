using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class TragicMandarinDuck : YuWanCardModel
{
    public TragicMandarinDuck() : base(
        baseCost: 1,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<TragicMandarinDuckPower>("GainStrength", 1, 1);
        WithPower<TragicMandarinDuckPower>("GainDexterity", 1, 1);
        WithVar("HpLoss", 1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TragicMandarinDuckPower>(new ThrowingPlayerChoiceContext(),Owner.Creature, 1, Owner.Creature, this);
    }
}
