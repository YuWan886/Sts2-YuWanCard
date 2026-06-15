using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfAngry : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Anger>()
    ];

    public WhatIfAngry() : base(true)
    {
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (side != CombatSide.Player || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        Flash();

        var anger = Owner.Creature.CombatState.CreateCard(ModelDb.Card<Anger>(), Owner);
        var addResult = await CardPileCmd.AddGeneratedCardToCombat(anger, PileType.Discard, Owner);
        CardCmd.PreviewCardPileAdd(addResult);
    }
}
