using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Config;

internal readonly record struct YuWanColorlessCardDefinition(
    string Key,
    Type CardType);

internal static class YuWanColorlessCardCatalog
{
    public const string SectionId = "colorless_cards";
    public const int ButtonsPerRow = 5;

    public static readonly IReadOnlyList<YuWanColorlessCardDefinition> Cards = BuildCards();

    private static readonly Dictionary<Type, YuWanColorlessCardDefinition> DefinitionsByType =
        Cards.ToDictionary(static definition => definition.CardType);

    private static readonly Dictionary<string, YuWanColorlessCardDefinition> DefinitionsByKey =
        Cards.ToDictionary(static definition => definition.Key, StringComparer.Ordinal);

    public static bool TryGetDefinition(Type cardType, out YuWanColorlessCardDefinition definition)
        => DefinitionsByType.TryGetValue(cardType, out definition);

    public static bool TryGetDefinition(string key, out YuWanColorlessCardDefinition definition)
        => DefinitionsByKey.TryGetValue(key, out definition);

    public static CardModel CreateCanonicalCard(YuWanColorlessCardDefinition definition)
        => ModelDb.GetById<CardModel>(ModelDb.GetId(definition.CardType));

    private static IReadOnlyList<YuWanColorlessCardDefinition> BuildCards()
    {
        return typeof(YuWanColorlessCardCatalog).Assembly
            .GetTypes()
            .Where(static type =>
                !type.IsAbstract
                && type.IsAssignableTo(typeof(CardModel))
                && type.GetCustomAttribute<PoolAttribute>()?.PoolType == typeof(ColorlessCardPool))
            .Select(Create)
            .OrderBy(static definition => definition.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private static YuWanColorlessCardDefinition Create(Type cardType)
        => new(
            ModelDb.GetId(cardType).Entry.RemovePrefix().ToLowerInvariant(),
            cardType);
}
