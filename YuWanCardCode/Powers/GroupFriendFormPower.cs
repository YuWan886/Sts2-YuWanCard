using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Powers;

public class GroupFriendFormPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("GroupFriendForm", 1m)];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        if (cardPlay.Card.Type == CardType.Power)
            return;

        var canonicalCard = cardPlay.Card.CanonicalInstance;
        if (canonicalCard == null)
            return;

        var combatState = Owner.CombatState;
        if (combatState == null)
            return;

        var newCard = combatState.CreateCard(canonicalCard, Owner.Player);
        for (int i = 0; i < cardPlay.Card.CurrentUpgradeLevel; i++)
        {
            CardCmd.Upgrade(newCard);
        }

        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Draw, addedByPlayer: true);
    }
}
