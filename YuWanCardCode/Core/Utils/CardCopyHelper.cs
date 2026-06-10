using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Utils;

internal static class CardCopyHelper
{
    public static CardModel CreateCopy(CardModel source, Player owner)
    {
        CardModel copy = CardModel.FromSerializable(source.ToSerializable());
        copy.Owner = owner;
        return copy;
    }

    public static CardModel? CreateCombatCopy(CardModel source, Player owner)
    {
        ICombatState? combatState = owner.Creature?.CombatState;
        if (combatState == null)
        {
            return null;
        }

        CardModel copy = CardModel.FromSerializable(source.ToSerializable());
        combatState.AddCard(copy, owner);
        return copy;
    }
}
