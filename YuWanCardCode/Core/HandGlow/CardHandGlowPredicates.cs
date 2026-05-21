using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.HandGlow;

/// <summary>
/// Reusable hand glow predicates based on common vanilla card patterns.
/// </summary>
public static class CardHandGlowPredicates
{
    /// <summary>
    /// True when the card owner's companion is missing.
    /// </summary>
    public static bool OwnerCompanionOstyMissing(CardModel card)
    {
        return card.Owner?.IsOstyMissing == true;
    }

    /// <summary>
    /// True when any of the owner's cards was exhausted this turn.
    /// </summary>
    public static bool AnyOfOwnersCardsExhaustedThisTurn(CardModel card)
    {
        var owner = card.Owner;
        var combatState = card.CombatState;
        var history = CombatManager.Instance?.History;
        if (owner == null || combatState == null || history == null)
            return false;

        return history.Entries
            .OfType<CardExhaustedEntry>()
            .Any(entry => entry.HappenedThisTurn(combatState) && entry.Card.Owner == owner);
    }

    /// <summary>
    /// True when this card has not finished a play this turn.
    /// </summary>
    public static bool ThisCardNotFinishedPlayThisTurn(CardModel card)
    {
        var combatState = card.CombatState;
        var history = CombatManager.Instance?.History;
        if (combatState == null || history == null)
            return false;

        return !history.CardPlaysFinished.Any(entry =>
            entry.CardPlay.Card == card && entry.HappenedThisTurn(combatState));
    }
}
