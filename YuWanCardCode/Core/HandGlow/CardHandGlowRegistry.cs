using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.HandGlow;

/// <summary>
/// Global registry for per-card-type hand glow rules.
/// </summary>
public static class CardHandGlowRegistry
{
    private static readonly ConcurrentDictionary<Type, CardHandGlowRules> RulesByCardType = new();

    /// <summary>
    /// Registers hand glow rules for a card type. Multiple registrations OR-merge channels.
    /// </summary>
    public static void Register<TCard>(CardHandGlowRules rules) where TCard : CardModel
    {
        Register(typeof(TCard), rules);
    }

    /// <summary>
    /// Registers hand glow rules for a concrete card type. Registration is blocked after content freeze.
    /// </summary>
    public static void Register(Type cardType, CardHandGlowRules rules)
    {
        ArgumentNullException.ThrowIfNull(cardType);

        if (ContentRegistry.IsFrozen)
        {
            throw new InvalidOperationException(
                "Cannot register card hand glow rules after content registration has been frozen.");
        }

        if (cardType.IsAbstract || !typeof(CardModel).IsAssignableFrom(cardType))
        {
            throw new ArgumentException(
                $"Type '{cardType.FullName}' must be a concrete subtype of {typeof(CardModel).FullName}.",
                nameof(cardType));
        }

        RulesByCardType.AddOrUpdate(cardType, rules, (_, existing) => existing.Or(rules));
    }

    /// <summary>
    /// Clears the registry. Intended for tests and hot-reload style tooling.
    /// </summary>
    public static void Clear()
    {
        RulesByCardType.Clear();
    }

    internal static bool EvaluateGold(CardModel card)
    {
        return EvaluateChannel(card, static (rules, model) => rules.MatchesGold(model));
    }

    internal static bool EvaluateRed(CardModel card)
    {
        return EvaluateChannel(card, static (rules, model) => rules.MatchesRed(model));
    }

    private static bool EvaluateChannel(CardModel card, Func<CardHandGlowRules, CardModel, bool> evaluator)
    {
        for (Type? type = card.GetType();
             type != null && typeof(CardModel).IsAssignableFrom(type);
             type = type.BaseType)
        {
            if (RulesByCardType.TryGetValue(type, out CardHandGlowRules rules) && evaluator(rules, card))
                return true;
        }

        return false;
    }
}
