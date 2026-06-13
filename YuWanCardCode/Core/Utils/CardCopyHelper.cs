using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Utils;

internal static class CardCopyHelper
{
    public static CardModel CreateCopy(CardModel source)
    {
        CardModel copy = CardModel.FromSerializable(source.ToSerializable());
        // Temporary/generated copies should keep runtime card state, but not keep mutating the source deck anchor.
        copy.DeckVersion = null;
        return copy;
    }

    public static CardModel? CreateCombatCopy(CardModel source, Player owner)
    {
        ICombatState? combatState = owner.Creature?.CombatState;
        if (combatState == null)
        {
            return null;
        }

        CardModel copy = CreateCopy(source);
        combatState.AddCard(copy, owner);
        return copy;
    }
}
