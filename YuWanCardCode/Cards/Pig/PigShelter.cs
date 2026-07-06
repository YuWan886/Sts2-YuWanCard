using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigShelter : YuWanCardModel, ITranscendenceCard
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
        foreach (var teammate in CombatState!.GetLivingPlayerCreatures())
        {
            await CreatureCmd.GainBlock(teammate, DynamicVars.Block, cardPlay);
        }
    }

    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<PerfectThing>();
}
