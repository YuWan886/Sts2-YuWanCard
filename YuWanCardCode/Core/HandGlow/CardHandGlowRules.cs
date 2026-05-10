using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.HandGlow;

/// <summary>
/// Declarative hand glow rules that mirror vanilla gold and red card highlight channels.
/// </summary>
public readonly struct CardHandGlowRules
{
    private static readonly ConcurrentDictionary<string, byte> LoggedFailures = new();

    /// <summary>
    /// Predicate for the gold hand glow channel.
    /// </summary>
    public Func<CardModel, bool>? GoldWhenBonusActive { get; init; }

    /// <summary>
    /// Predicate for the red hand glow channel.
    /// </summary>
    public Func<CardModel, bool>? RedWhenHandWarning { get; init; }

    /// <summary>
    /// Creates a gold-only glow rule.
    /// </summary>
    public static CardHandGlowRules Gold(Func<CardModel, bool> whenBonusActive)
    {
        ArgumentNullException.ThrowIfNull(whenBonusActive);
        return new CardHandGlowRules
        {
            GoldWhenBonusActive = whenBonusActive
        };
    }

    /// <summary>
    /// Creates a red-only glow rule.
    /// </summary>
    public static CardHandGlowRules Red(Func<CardModel, bool> whenHandWarning)
    {
        ArgumentNullException.ThrowIfNull(whenHandWarning);
        return new CardHandGlowRules
        {
            RedWhenHandWarning = whenHandWarning
        };
    }

    /// <summary>
    /// Creates a rule with both glow channels.
    /// </summary>
    public static CardHandGlowRules GoldAndRed(
        Func<CardModel, bool>? goldWhenBonusActive,
        Func<CardModel, bool>? redWhenHandWarning)
    {
        return new CardHandGlowRules
        {
            GoldWhenBonusActive = goldWhenBonusActive,
            RedWhenHandWarning = redWhenHandWarning
        };
    }

    /// <summary>
    /// OR-merges two rule sets.
    /// </summary>
    public CardHandGlowRules Or(CardHandGlowRules other)
    {
        return new CardHandGlowRules
        {
            GoldWhenBonusActive = CombineOr(GoldWhenBonusActive, other.GoldWhenBonusActive),
            RedWhenHandWarning = CombineOr(RedWhenHandWarning, other.RedWhenHandWarning)
        };
    }

    /// <summary>
    /// Evaluates the gold glow predicate safely.
    /// </summary>
    public bool MatchesGold(CardModel card)
    {
        return Evaluate(GoldWhenBonusActive, card, "gold");
    }

    /// <summary>
    /// Evaluates the red glow predicate safely.
    /// </summary>
    public bool MatchesRed(CardModel card)
    {
        return Evaluate(RedWhenHandWarning, card, "red");
    }

    private static Func<CardModel, bool>? CombineOr(Func<CardModel, bool>? first, Func<CardModel, bool>? second)
    {
        if (first == null)
            return second;

        return second == null ? first : card => first(card) || second(card);
    }

    private static bool Evaluate(Func<CardModel, bool>? predicate, CardModel card, string channel)
    {
        if (predicate == null)
            return false;

        try
        {
            return predicate(card);
        }
        catch (Exception ex)
        {
            string key = $"{card.GetType().FullName}:{channel}";
            if (LoggedFailures.TryAdd(key, 0))
            {
                MainFile.Logger.Error(
                    $"Card hand glow {channel} predicate failed for {card.GetType().Name}: {ex.Message}");
            }
            return false;
        }
    }
}
