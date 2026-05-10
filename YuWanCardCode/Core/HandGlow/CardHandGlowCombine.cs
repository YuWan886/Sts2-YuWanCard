using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.HandGlow;

/// <summary>
/// Combinators for card hand glow predicates.
/// </summary>
public static class CardHandGlowCombine
{
    /// <summary>
    /// Logical OR for all non-null predicates.
    /// </summary>
    public static Func<CardModel, bool> Or(params Func<CardModel, bool>?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return card => parts.OfType<Func<CardModel, bool>>().Any(predicate => predicate(card));
    }

    /// <summary>
    /// Logical AND for all non-null predicates. Returns <c>true</c> if every supplied predicate matches.
    /// </summary>
    public static Func<CardModel, bool> And(params Func<CardModel, bool>?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        Func<CardModel, bool>[] predicates = parts
            .Where(static predicate => predicate != null)
            .Cast<Func<CardModel, bool>>()
            .ToArray();

        if (predicates.Length == 0)
            return static _ => true;

        return card => predicates.All(predicate => predicate(card));
    }
}
