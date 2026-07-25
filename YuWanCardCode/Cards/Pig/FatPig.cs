using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class FatPig : YuWanCardModel
{
    public FatPig() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithBlock(15, 3);
        WithPower<PlatingPower>(3);
        WithKeywords(CardKeyword.Exhaust);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["PlatingPower"].IntValue,
            Owner.Creature,
            this);

        VfxUtils.PlayStaticVfxAtCreatureTop(Owner.Creature);
    }
}
