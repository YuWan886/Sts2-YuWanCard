using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Utils;

internal static class CombatCardStateHelper
{
    public static CardModel? ResolveSelectedHandCard(Player? owner, CardModel? selectedCard)
    {
        if (owner == null || selectedCard == null)
        {
            return null;
        }

        IReadOnlyList<CardModel> handCards = PileType.Hand.GetPile(owner).Cards;
        if (handCards.Any(card => ReferenceEquals(card, selectedCard)))
        {
            return selectedCard;
        }

        if (NetCombatCardDb.Instance.TryGetCardId(selectedCard, out uint combatCardId)
            && NetCombatCardDb.Instance.TryGetCard(combatCardId, out CardModel? combatCard)
            && combatCard != null
            && handCards.Any(card => ReferenceEquals(card, combatCard)))
        {
            return combatCard;
        }

        SerializableCard serializedCard = selectedCard.ToSerializable();
        return handCards.FirstOrDefault(card => MatchesSelection(card, serializedCard, selectedCard));
    }

    private static bool MatchesSelection(CardModel candidate, SerializableCard serializedCard, CardModel selectedCard)
    {
        if (!candidate.IsMutable || candidate.Pile?.Type != PileType.Hand)
        {
            return false;
        }

        if (!candidate.ToSerializable().Equals(serializedCard))
        {
            return false;
        }

        return candidate.EnergyCost?.GetResolved() == selectedCard.EnergyCost?.GetResolved();
    }
}
