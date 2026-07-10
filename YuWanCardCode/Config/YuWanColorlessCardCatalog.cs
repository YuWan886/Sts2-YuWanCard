using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Config;

internal readonly record struct YuWanColorlessCardDefinition(
    string Key,
    Type CardType);

internal static class YuWanColorlessCardCatalog
{
    public const string SectionId = "colorless_cards";
    public const int ButtonsPerRow = 5;

    private static readonly HashSet<CardType> DoctorPigExcludedTypes =
    [
        CardType.None,
        CardType.Status,
        CardType.Curse,
        CardType.Quest
    ];

    private static readonly HashSet<CardRarity> DoctorPigExcludedRarities =
    [
        CardRarity.None,
        CardRarity.Basic,
        CardRarity.Ancient,
        CardRarity.Token,
        CardRarity.Status,
        CardRarity.Curse,
        CardRarity.Quest
    ];

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

    public static IReadOnlyList<CardModel> GetUnlockedCanonicalCards(Player player)
    {
        var unlockedIds = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(static card => card.Id)
            .ToHashSet();

        return Cards
            .Select(CreateCanonicalCard)
            .Where(card => unlockedIds.Contains(card.Id))
            .ToArray();
    }

    public static IReadOnlyList<CardModel> GetUnlockedCanonicalCards(IRunState runState)
    {
        var unlockedIds = runState.Players
            .SelectMany(static player => ModelDb.CardPool<ColorlessCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Select(static card => card.Id))
            .ToHashSet();

        return Cards
            .Select(CreateCanonicalCard)
            .Where(card => unlockedIds.Contains(card.Id))
            .ToArray();
    }

    public static IReadOnlyList<CardModel> GetUnlockedDoctorPigCards(Player player)
    {
        return GetUnlockedCanonicalCards(player)
            .Where(IsDoctorPigEligibleCard)
            .ToArray();
    }

    public static IReadOnlyList<CardModel> GetUnlockedDoctorPigCards(IRunState runState)
    {
        return GetUnlockedCanonicalCards(runState)
            .Where(IsDoctorPigEligibleCard)
            .ToArray();
    }

    public static bool IsDoctorPigEligibleCard(CardModel card)
    {
        return !DoctorPigExcludedTypes.Contains(card.Type)
               && !DoctorPigExcludedRarities.Contains(card.Rarity);
    }

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
