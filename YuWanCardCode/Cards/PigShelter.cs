using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigShelter : YuWanCardModel
{
    public PigShelter() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Basic,
        target: TargetType.AllAllies)
    {
        WithBlock(4);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature);
        foreach (var teammate in teammates)
        {
            await CreatureCmd.GainBlock(teammate, DynamicVars.Block, cardPlay);
        }
    }
}
