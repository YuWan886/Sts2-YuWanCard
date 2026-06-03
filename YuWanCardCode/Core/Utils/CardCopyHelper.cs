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
}
