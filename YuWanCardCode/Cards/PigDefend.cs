using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigDefend : YuWanCardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IroncladCardPool>();

    public PigDefend() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Basic,
        target: TargetType.Self)
    {
        WithBlock(5);
        WithTags(CardTag.Defend);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
}
