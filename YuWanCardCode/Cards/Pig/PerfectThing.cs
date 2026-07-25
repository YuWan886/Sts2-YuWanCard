using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PerfectThing : YuWanCardModel
{
    public PerfectThing() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies)
    {
        WithBlock(10);
        WithVar("ExtraTurns", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var playerCreature in CombatState!.Allies.Where(creature => creature.IsAlive))
        {
            await CreatureCmd.GainBlock(playerCreature, DynamicVars.Block, cardPlay);
        }

        await PowerCmd.Apply<PerfectThingPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["ExtraTurns"].IntValue,
            Owner.Creature,
            this);
    }
}
