using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Enchantments;

public sealed class Loyal : EnchantmentModel
{
    public override bool ShowAmount => true;
    public override bool IsStackable => false;

    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card))
        {
            return false;
        }

        return !card.Keywords.Contains(CardKeyword.Unplayable);
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

        var currentPile = Card.Pile?.Type;
        if (currentPile == null)
        {
            return;
        }

        if (currentPile != PileType.Hand)
        {
            await CardPileCmd.Add(Card, PileType.Hand, skipVisuals: true);
        }

        var target = GetTargetForCard(Card, player.Creature.CombatState);
        await CardCmd.AutoPlay(choiceContext, Card, target, AutoPlayType.Default, skipXCapture: true, skipCardPileVisuals: true);
    }

    private Creature? GetTargetForCard(CardModel card, CombatState? combatState)
    {
        if (combatState == null || card.Owner == null)
        {
            return null;
        }

        var rng = card.Owner.RunState?.Rng;
        if (rng == null)
        {
            return null;
        }

        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy
                => rng.CombatTargets.NextItem(combatState.HittableEnemies),
            TargetType.AnyPlayer
                => rng.CombatTargets.NextItem(combatState.Players.Where(p => p.Creature.IsAlive).Select(p => p.Creature)),
            TargetType.AnyAlly or TargetType.AllAllies
                => rng.CombatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive)),
            TargetType.Self => card.Owner.Creature,
            _ => null
        };
    }
}
