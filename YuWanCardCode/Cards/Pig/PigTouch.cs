using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigTouch : YuWanCardModel
{
    public PigTouch() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: CustomTargetType.AnyPigMinion)
    {
        WithHeal(6, 3);
        WithPower<PigTouchPower>("Strength", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is { IsDead: false } pig)
        {
            await CreatureCmd.Heal(pig, DynamicVars.Heal.BaseValue);
            await PowerCmd.Apply<PigTouchPower>(new ThrowingPlayerChoiceContext(), pig, DynamicVars["Strength"].IntValue, Owner.Creature, this);
        }
    }
}
