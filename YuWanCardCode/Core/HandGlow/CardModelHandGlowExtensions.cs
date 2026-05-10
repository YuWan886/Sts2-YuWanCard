using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.HandGlow;

/// <summary>
/// Extension helpers for common hand glow checks used in card logic.
/// </summary>
public static class CardModelHandGlowExtensions
{
    /// <summary>
    /// Returns whether the card owner's companion is missing.
    /// </summary>
    public static bool HandGlowOwnerCompanionOstyMissing(this CardModel card)
    {
        return CardHandGlowPredicates.OwnerCompanionOstyMissing(card);
    }

    /// <summary>
    /// Returns whether any of the owner's cards was exhausted this turn.
    /// </summary>
    public static bool HandGlowAnyOfOwnersCardsExhaustedThisTurn(this CardModel card)
    {
        return CardHandGlowPredicates.AnyOfOwnersCardsExhaustedThisTurn(card);
    }

    /// <summary>
    /// Returns whether this card has not finished a play this turn.
    /// </summary>
    public static bool HandGlowThisCardNotFinishedPlayThisTurn(this CardModel card)
    {
        return CardHandGlowPredicates.ThisCardNotFinishedPlayThisTurn(card);
    }
}
