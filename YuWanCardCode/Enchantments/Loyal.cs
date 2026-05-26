using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Enchantments;

public sealed class Loyal : YuWanEnchantmentModel
{
    public override bool ShowAmount => true;
    public override bool IsStackable => false;

    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card))
        {
            return false;
        }

        if (card.Type == CardType.Power)
        {
            return false;
        }

        if (card.EnergyCost.CostsX)
        {
            return false;
        }

        if (card.Keywords.Contains(CardKeyword.Unplayable))
        {
            return false;
        }

        if (card.Keywords.Contains(CardKeyword.Exhaust))
        {
            return false;
        }

        if (card.GetEnchantedReplayCount() > 0)
        {
            return false;
        }

        return true;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Card == null || Card.Owner?.Creature == null)
        {
            return;
        }

        if (player.Creature != Card.Owner.Creature)
        {
            return;
        }

        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        if (Card.HasBeenRemovedFromState)
        {
            return;
        }

        if (Card.Pile?.Type is null or PileType.Play)
        {
            return;
        }

        if (Card.Pile.Type == PileType.Hand)
        {
            return;
        }

        await CardPileCmd.Add(Card, PileType.Hand, skipVisuals: true);

        var target = GetTargetForCard(Card, player.Creature.CombatState);
        await CardCmd.AutoPlay(choiceContext, Card, target, AutoPlayType.Default, skipXCapture: true, skipCardPileVisuals: true);
    }

    private Creature? GetTargetForCard(CardModel card, CombatState? combatState)
    {
        return combatState == null ? null : card.PickRandomTarget();
    }
}
