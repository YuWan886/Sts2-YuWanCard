using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigBlock : YuWanCardModel
{
    public PigBlock() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithBlock(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handCount = PileType.Hand.GetPile(Owner).Cards.Count;
        for (int i = 0; i < handCount; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }
}
