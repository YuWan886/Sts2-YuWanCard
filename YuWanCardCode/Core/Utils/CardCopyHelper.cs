using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Utils;

internal static class CardCopyHelper
{
    public static CardModel CreateCopy(CardModel source, Player owner)
    {
        CardModel copy = (CardModel)source.MutableClone();
        copy.Owner = owner;
        // Temporary/generated copies should keep runtime card state, but not keep mutating the source deck anchor.
        copy.DeckVersion = null;
        return copy;
    }

    public static CardModel? CreateCombatCopy(CardModel source, Player owner)
    {
        CombatState? combatState = owner.Creature?.CombatState;
        if (combatState == null)
        {
            return null;
        }

        CardModel copy = CreateCopy(source, owner);
        combatState.AddCard(copy, owner);
        return copy;
    }
}
