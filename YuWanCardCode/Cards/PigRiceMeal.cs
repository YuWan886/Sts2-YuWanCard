using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigRiceMeal : YuWanCardModel
{
    public PigRiceMeal() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithBlock(6);
        WithTip(typeof(PigFeed));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var statusCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.Rarity == CardRarity.Status)
            .ToList();

        if (statusCards.Count == 0 || CombatState == null) return;

        var transformations = new List<CardTransformation>();

        foreach (var statusCard in statusCards)
        {
            var newCard = CombatState.CreateCard<PigFeed>(Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(newCard);
            }
            transformations.Add(new CardTransformation(statusCard, newCard));
        }

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, Owner.RunState.Rng.CombatCardGeneration);
        }
    }
}
